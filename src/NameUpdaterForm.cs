using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму приложения
	/// </summary>
	public partial class NameUpdaterForm: Form
		{
		private enum ComparisonModes
			{
			Unused = -1,

			GreaterThan = 0,
			LessThan = 1,
			EqualTo = 2,
			NotEqualTo = 3
			}

		private enum SubstitutionTypes
			{
			SerialNumber = 0,

			ModificationDateTime = 10,
			OldFileName = 11
			}

		// Переменные
		private char[] charactersCriteriaSplitter = ['|'];
		private string[] renameMasks = [
			"⁰",
			"¹",
			"²",
			"³",
			"⁴",
			"⁵",
			"⁶",
			"⁷",
			"⁸",
			"⁹",

			"ᴹ",
			"ᴾ"
			];

		private string sourceDirectory;
		private bool includeSubdirs;
		private List<string> nameCriteria = [];
		private long sizeComparisonValue;
		private ComparisonModes sizeComparisonMode;
		private DateTime dateComparisonValue;
		private ComparisonModes dateComparisonMode;

		private string destinationDirectory;
		private bool copyOnly;
		private string destinationName;
		private List<string[]> destinationNameSubs = [];

		private List<string> sourceFileNames = [];
		private List<FileInfo> sourceFileInfo = [];
		private List<string> destinationFileNames = [];

		private string failedFileName;

		/// <summary>
		/// Конструктор. Настраивает главную форму приложения
		/// </summary>
		public NameUpdaterForm ()
			{
			// Инициализация
			InitializeComponent ();
			RDGenerics.LoadWindowDimensions (this);
			this.Text = RDGenerics.DefaultAssemblyVisibleName;

			// Загрузка начальных значений
			LocalizeForm ();

			CharactersCriteriaFlag_CheckedChanged (null, null);
			SizeCriteriaFlag_CheckedChanged (null, null);
			DateCriteriaFlag_CheckedChanged (null, null);
			DateCriteriaField.Value = DateTime.Now;
			MoveToActionFlag_CheckedChanged (null, null);
			RenameToActionFlag_CheckedChanged (null, null);

			DateCriteriaField.MaxDate = RDGenerics.MaximumDatePickerValue;
			}

		// Метод локализует форму
		private void LocalizeForm ()
			{
			SizeCriteriaCombo.Items.Clear ();
			SizeCriteriaCombo.Items.Add ("greater than");
			SizeCriteriaCombo.Items.Add ("less than");
			SizeCriteriaCombo.Items.Add ("equal to");
			SizeCriteriaCombo.Items.Add ("not equal to");
			SizeCriteriaCombo.SelectedIndex = 0;

			SizeCriteriaUnitCombo.Items.Clear ();
			SizeCriteriaUnitCombo.Items.Add ("bytes");
			SizeCriteriaUnitCombo.Items.Add ("kilobytes");
			SizeCriteriaUnitCombo.Items.Add ("megabytes");
			SizeCriteriaUnitCombo.Items.Add ("gigabytes");
			SizeCriteriaUnitCombo.SelectedIndex = 2;

			DateCriteriaCombo.Items.Clear ();
			DateCriteriaCombo.Items.Add ("greater than");
			DateCriteriaCombo.Items.Add ("less than");
			DateCriteriaCombo.Items.Add ("equal to");
			DateCriteriaCombo.Items.Add ("not equal to");
			DateCriteriaCombo.SelectedIndex = 0;

			BAbout.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout);
			BLanguage.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceLanguage);
			BExit.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);
			}

		// Справка
		private void MHelp_Click (object sender, EventArgs e)
			{
			RDInterface.ShowAbout (false);
			}

		// Язык интерфейса
		private void MLanguage_Click (object sender, EventArgs e)
			{
			if (RDInterface.MessageBox ())
				LocalizeForm ();
			}

		// Закрытие окна
		private void MExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Закрытие окна
		private void NameUpdaterForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			RDGenerics.SaveWindowDimensions (this);
			}

		// Выбор исходной директории
		private void SourceDirectoryButton_Click (object sender, EventArgs e)
			{
			if (!string.IsNullOrWhiteSpace (SourceDirectoryField.Text))
				FBDialog.SelectedPath = SourceDirectoryField.Text;

			if (FBDialog.ShowDialog () == DialogResult.OK)
				SourceDirectoryField.Text = FBDialog.SelectedPath;
			}

		// Включение критерия «содержит символы или слова в имени»
		private void CharactersCriteriaFlag_CheckedChanged (object sender, EventArgs e)
			{
			CharactersCriteriaButton.Enabled = CharactersCriteriaField.Enabled = CharactersCriteriaFlag.Checked;
			}

		// Добавление признака ИЛИ в поле критерия «содержит символы или слова в имени»
		private void CharactersCriteriaButton_Click (object sender, EventArgs e)
			{
			try
				{
				CharactersCriteriaField.Text += charactersCriteriaSplitter[0];
				}
			catch { }

			CharactersCriteriaField.Focus ();
			CharactersCriteriaField.Select (CharactersCriteriaField.Text.Length, 0);
			}

		// Включение критерия «имеет размер»
		private void SizeCriteriaFlag_CheckedChanged (object sender, EventArgs e)
			{
			SizeCriteriaCombo.Enabled = SizeCriteriaField.Enabled = SizeCriteriaUnitCombo.Enabled =
				SizeCriteriaFlag.Checked;
			}

		// Изменение границы размера файла
		private void SizeCriteriaUnitCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			ulong v = (1ul << 32);
			v >>= (10 * SizeCriteriaUnitCombo.SelectedIndex);
			SizeCriteriaField.Maximum = v;
			}

		// Включение критерия «имеет дату изменения»
		private void DateCriteriaFlag_CheckedChanged (object sender, EventArgs e)
			{
			DateCriteriaCombo.Enabled = DateCriteriaField.Enabled = DateCriteriaFlag.Checked;
			}

		// Включение действия «переместить»
		private void MoveToActionFlag_CheckedChanged (object sender, EventArgs e)
			{
			MoveToActionButton.Enabled = MoveToActionField.Enabled = MoveToRadio.Enabled =
				CopyToRadio.Enabled = MoveToActionFlag.Checked;
			}

		// Выбор конечной директории
		private void MoveToActionButton_Click (object sender, EventArgs e)
			{
			if (!string.IsNullOrWhiteSpace (MoveToActionField.Text))
				FBDialog.SelectedPath = MoveToActionField.Text;

			if (FBDialog.ShowDialog () == DialogResult.OK)
				MoveToActionField.Text = FBDialog.SelectedPath;
			}

		// Включение действия «переименовать»
		private void RenameToActionFlag_CheckedChanged (object sender, EventArgs e)
			{
			RenameToActionDateButton.Enabled = RenameToActionNumberButton.Enabled =
				RenameToActionField.Enabled = Label05.Enabled = RenameToActionFlag.Checked;
			}

		// Добавление порядкового номера
		private void RenameToActionNumberButton_Click (object sender, EventArgs e)
			{
			string s = RDInterface.MessageBox ("Enter the minimum number length (number of padding zeros)",
				true, 1, "3");
			if (string.IsNullOrWhiteSpace (s))
				return;

			if (!"0123456789".Contains (s))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Enter a correct length and try again");
				return;
				}

			int idx = int.Parse (s);
			try
				{
				RenameToActionField.Text += renameMasks[idx];
				}
			catch { }

			RenameToActionField.Focus ();
			RenameToActionField.Select (RenameToActionField.Text.Length, 0);
			}

		// Добавление старого имени файла
		private void RenameToActionNameButton_Click (object sender, EventArgs e)
			{
			try
				{
				RenameToActionField.Text += renameMasks[11];
				}
			catch { }

			RenameToActionField.Focus ();
			RenameToActionField.Select (RenameToActionField.Text.Length, 0);
			}

		// Добавление даты
		private void RenameToActionDateButton_Click (object sender, EventArgs e)
			{
			try
				{
				RenameToActionField.Text += renameMasks[10];
				}
			catch { }

			RenameToActionField.Focus ();
			RenameToActionField.Select (RenameToActionField.Text.Length, 0);
			}

		// Запуск действия
		private static SubstitutionTypes GetSubsType (byte SubsMark)
			{
			switch (SubsMark)
				{
				case 10:
					return SubstitutionTypes.ModificationDateTime;

				case 11:
					return SubstitutionTypes.OldFileName;

				// 0 - 9
				default:
					return SubstitutionTypes.SerialNumber;
				}
			}

		private void StartButton_Click (object sender, EventArgs e)
			{
			#region Контроль значений

			// Контроль исходной директории
			if (string.IsNullOrWhiteSpace (SourceDirectoryField.Text))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Source directory is not specified");
				return;
				}

			if (!Directory.Exists (SourceDirectoryField.Text))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Source directory is not available");
				return;
				}

			sourceDirectory = SourceDirectoryField.Text;
			includeSubdirs = IncludeSubdirectoriesFlag.Checked;

			// Контроль фильтра по имени файла
			nameCriteria.Clear ();
			if (CharactersCriteriaFlag.Checked)
				{
				string[] crt = CharactersCriteriaField.Text.Split (charactersCriteriaSplitter,
					StringSplitOptions.RemoveEmptyEntries);
				if (crt.Length < 1)
					{
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
						"File name criteria doesn’t contain any applicable characters or words");
					return;
					}

				nameCriteria.AddRange (crt);
				}

			// Контроль фильтра по размеру файла
			if (SizeCriteriaFlag.Checked)
				{
				sizeComparisonMode = (ComparisonModes)SizeCriteriaCombo.SelectedIndex;
				sizeComparisonValue = (long)SizeCriteriaField.Value * (1l << (10 * SizeCriteriaUnitCombo.SelectedIndex));
				}
			else
				{
				sizeComparisonMode = ComparisonModes.Unused;
				}

			// Контроль фильтра по дате изменения файла
			if (DateCriteriaFlag.Checked)
				{
				dateComparisonMode = (ComparisonModes)DateCriteriaCombo.SelectedIndex;
				dateComparisonValue = new DateTime (DateCriteriaField.Value.Year, DateCriteriaField.Value.Month,
					DateCriteriaField.Value.Day, 0, 0, 0);
				}
			else
				{
				dateComparisonMode = ComparisonModes.Unused;
				}

			// Контроль действий над файлами
			if (!MoveToActionFlag.Checked && !RenameToActionFlag.Checked)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"No actions selected to perform with files");
				return;
				}

			// Контроль конечной директории
			if (MoveToActionFlag.Checked)
				{
				if (string.IsNullOrWhiteSpace (MoveToActionField.Text))
					{
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"Destination directory is not specified");
					return;
					}

				if (!Directory.Exists (MoveToActionField.Text))
					{
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"Destination directory is not available");
					return;
					}

				destinationDirectory = MoveToActionField.Text;
				}
			else
				{
				destinationDirectory = sourceDirectory;
				}

			if (!destinationDirectory.EndsWith ('\\'))
				destinationDirectory += "\\";
			copyOnly = MoveToActionFlag.Checked && CopyToRadio.Checked;

			// Контроль конечного имени файла
			destinationNameSubs.Clear ();
			destinationName = "";
			if (RenameToActionFlag.Checked)
				{
				int fmtIdx = 0;
				bool hasSerialNumber = false;

				for (int i = 0; i < RenameToActionField.Text.Length; i++)
					{
					int maskIdx = renameMasks.IndexOf (RenameToActionField.Text[i].ToString ());
					if (maskIdx < 0)
						{
						destinationName += RenameToActionField.Text[i];
						continue;
						}

					string fmt = "%" + fmtIdx.ToString ("D2") + "%";
					fmtIdx++;

					destinationName += fmt;

					SubstitutionTypes sType = GetSubsType ((byte)maskIdx);
					string sTypeString = ((byte)sType).ToString ();

					switch (sType)
						{
						case SubstitutionTypes.SerialNumber:
						default:
							hasSerialNumber = true;
							destinationNameSubs.Add ([fmt, sTypeString, "D" + maskIdx.ToString ()]);
							break;

						case SubstitutionTypes.ModificationDateTime:
							destinationNameSubs.Add ([fmt, sTypeString, "dd-MM-yyyy"]);
							break;

						case SubstitutionTypes.OldFileName:
							destinationNameSubs.Add ([fmt, sTypeString, ""]);
							break;
						}
					}

				if (!hasSerialNumber)
					{
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
						"The file renaming can’t be used without at least one serial number position");
					return;
					}
				}

			#endregion

			// Сбор сведений о файлах
			RDInterface.RunWork (CollectFileData, null, "Collecting file data...", RDRunWorkFlags.CaptionInTheMiddle);

			if (sourceFileNames.Count < 1)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
					"Can’t find files matching the specified criteria");
				return;
				}

			// Формирование имён конечных файлов
			destinationFileNames.Clear ();
			for (int i = 0; i < sourceFileNames.Count; i++)
				{
				string name;
				if (string.IsNullOrWhiteSpace (destinationName))
					name = Path.GetFileNameWithoutExtension (sourceFileNames[i]);
				else
					name = destinationName;

				string ext = Path.GetExtension (sourceFileNames[i]);
				if (string.IsNullOrWhiteSpace (ext))
					ext = "";

				for (int j = 0; j < destinationNameSubs.Count; j++)
					{
					SubstitutionTypes sType = (SubstitutionTypes)byte.Parse (destinationNameSubs[j][1]);

					switch (sType)
						{
						case SubstitutionTypes.SerialNumber:
						default:
							name = name.Replace (destinationNameSubs[j][0], (i + 1).ToString (destinationNameSubs[j][2]));
							break;

						case SubstitutionTypes.OldFileName:
							name = name.Replace (destinationNameSubs[j][0],
								Path.GetFileNameWithoutExtension (sourceFileNames[i]));
							break;

						case SubstitutionTypes.ModificationDateTime:
							name = name.Replace (destinationNameSubs[j][0],
								sourceFileInfo[i].LastWriteTime.ToString (destinationNameSubs[j][2]));
							break;
						}
					}

				destinationFileNames.Add (destinationDirectory + name + ext);
				}

			// Выполнение операции
			RDInterface.RunWork (CopyMoveFiles, null, "Processing files...", RDRunWorkFlags.AllowOperationAbort |
				RDRunWorkFlags.CaptionInTheMiddle);
			switch (RDInterface.WorkResultAsInteger)
				{
				case -1:
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
						"Failed to move / copy file:" + RDLocale.RN + failedFileName + RDLocale.RNRN +
						"Check that it is accessible, you has write permissions, and that there is no file " +
						"with the same name in the destination directory");
					break;

				default:
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"Operation has been interrupted by user");
					break;

				case 0:
					RDInterface.MessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
						"Operation has been completed successfully", 1500);
					break;
				}
			}

		// Метод сбора сведений об исходных файлах
		private void CollectFileData (object sender, DoWorkEventArgs e)
			{
			// Первичный сбор списка
			sourceFileNames.Clear ();
			sourceFileInfo.Clear ();
			SearchOption so = includeSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

			if (nameCriteria.Count < 1)
				{
				try
					{
					sourceFileNames.AddRange (Directory.GetFiles (sourceDirectory, "*", so));
					}
				catch { }
				}
			else
				{
				for (int i = 0; i < nameCriteria.Count; i++)
					{
					try
						{
						sourceFileNames.AddRange (Directory.GetFiles (sourceDirectory, "*" +
							nameCriteria[i] + "*", so));
						}
					catch { }
					}
				}

			// Сбор сведений о файлах
			/*List<FileInfo> fi = [];*/

			bool dateCriteria = (dateComparisonMode != ComparisonModes.Unused);
			bool sizeCriteria = (sizeComparisonMode != ComparisonModes.Unused);

			/*if (dateCriteria || sizeCriteria)
				{*/
			for (int i = 0; i < sourceFileNames.Count; i++)
				sourceFileInfo.Add (new FileInfo (sourceFileNames[i]));
			/*}*/

			// Прогон по размерам и датам
			for (int i = sourceFileNames.Count - 1; i >= 0; i--)
				{
				bool fit = true;

				if (sizeCriteria)
					{
					long len = sourceFileInfo[i].Length;

					switch (sizeComparisonMode)
						{
						case ComparisonModes.GreaterThan:
							fit = fit && (len > sizeComparisonValue);
							break;

						case ComparisonModes.LessThan:
							fit = fit && (len < sizeComparisonValue);
							break;

						case ComparisonModes.EqualTo:
							fit = fit && (len == sizeComparisonValue);
							break;

						case ComparisonModes.NotEqualTo:
							fit = fit && (len != sizeComparisonValue);
							break;
						}
					}

				if (dateCriteria)
					{
					DateTime dt = sourceFileInfo[i].LastWriteTime;
					dt = new DateTime (dt.Year, dt.Month, dt.Day, 0, 0, 0);

					switch (dateComparisonMode)
						{
						case ComparisonModes.GreaterThan:
							fit = fit && (dt > dateComparisonValue);
							break;

						case ComparisonModes.LessThan:
							fit = fit && (dt < dateComparisonValue);
							break;

						case ComparisonModes.EqualTo:
							fit = fit && (dt == dateComparisonValue);
							break;

						case ComparisonModes.NotEqualTo:
							fit = fit && (dt != dateComparisonValue);
							break;
						}
					}

				if (!fit)
					{
					sourceFileInfo.RemoveAt (i);
					sourceFileNames.RemoveAt (i);
					}
				}

			// Завершено
			e.Result = 0;
			}

		// Метод копирования / переноса файлов
		private void CopyMoveFiles (object sender, DoWorkEventArgs e)
			{
			BackgroundWorker bw = (BackgroundWorker)sender;

			for (int i = 0; i < sourceFileNames.Count; i++)
				{
				// Завершение работы, если получено требование от диалога
				if (bw.CancellationPending)
					{
					e.Cancel = true;
					return;
					}

				// Оповещение о прогрессе
				bw.ReportProgress ((int)RDWorkerForm.ProgressBarSize * (i + 1) / sourceFileNames.Count,
					"Processing file" + RDLocale.RN + (i + 1).ToString () + " out of " +
					sourceFileNames.Count.ToString ());

				// Выполнение
				try
					{
					if (copyOnly)
						File.Copy (sourceFileNames[i], destinationFileNames[i], false);
					else
						File.Move (sourceFileNames[i], destinationFileNames[i], false);
					}
				catch
					{
					failedFileName = Path.GetFileName (sourceFileNames[i]);
					e.Result = -1;
					return;
					}
				}

			e.Result = 0;
			}
		}
	}
