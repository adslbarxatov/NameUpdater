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
		// Доступные варианты подстановки
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
		private ulong startNumber;

		private List<string> sourceFileNames = [];
		private List<FileInfo> sourceFileInfo = [];
		private List<string> destinationFileNames = [];

		private string failedFileName;

		private NameUpdaterProfilesSet profilesSet = new NameUpdaterProfilesSet ();

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

			ReloadProfiles ();
			}

		private void ReloadProfiles ()
			{
			ProfileCombo.Items.Clear ();
			ProfileCombo.Items.AddRange (profilesSet.ProfileNames);

			ProfileRemoveButton.Enabled = ProfileLoadButton.Enabled = ProfileCombo.Enabled =
				(ProfileCombo.Items.Count > 0);
			if (ProfileCombo.Enabled)
				ProfileCombo.SelectedIndex = 0;
			}

		// Метод локализует форму
		private void LocalizeForm ()
			{
			SizeCriteriaCombo.Items.Clear ();
			for (int i = 0; i < (int)ComparisonModes._Size_; i++)
				SizeCriteriaCombo.Items.Add (RDLocale.GetText ("Comparison" + i.ToString ("D2")));
			SizeCriteriaCombo.SelectedIndex = 0;

			SizeCriteriaUnitCombo.Items.Clear ();
			for (int i = 0; i < 4; i++)
				SizeCriteriaUnitCombo.Items.Add (RDLocale.GetText ("SizeUnit" + i.ToString ("D2")));
			SizeCriteriaUnitCombo.SelectedIndex = 1;	// Kb

			DateCriteriaCombo.Items.Clear ();
			for (int i = 0; i < (int)ComparisonModes._Size_; i++)
				DateCriteriaCombo.Items.Add (RDLocale.GetText ("Comparison" + i.ToString ("D2")));
			DateCriteriaCombo.SelectedIndex = 0;

			BAbout.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout);
			BLanguage.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceLanguage);
			BExit.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);

			RDLocale.SetControlText (Label01);
			RDLocale.SetControlText (IncludeSubdirectoriesFlag);
			RDLocale.SetControlText (Label02);
			RDLocale.SetControlText (CharactersCriteriaFlag);
			RDLocale.SetControlText (CharactersCriteriaButton);
			RDLocale.SetControlText (SizeCriteriaFlag);
			RDLocale.SetControlText (DateCriteriaFlag);

			RDLocale.SetControlText (Label04);
			RDLocale.SetControlText (MoveToActionFlag);
			RDLocale.SetControlText (MoveToRadio);
			RDLocale.SetControlText (CopyToRadio);
			RDLocale.SetControlText (RenameToActionFlag);
			RDLocale.SetControlText (Label05);
			RDLocale.SetControlText (RenameToActionNumberButton);
			RDLocale.SetControlText (RenameToActionDateButton);
			RDLocale.SetControlText (RenameToActionNameButton);

			RDLocale.SetControlText (StartButton);
			RDLocale.SetControlText (ProfileLabel);

			RDLocale.SetControlText (CreateDirectoryFlag);
			RDLocale.SetControlText (Label10);
			RDLocale.SetControlText (InputPage);
			RDLocale.SetControlText (OutputPage);
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

			RDInterface.SetFocusToTextbox (CharactersCriteriaField);
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
				CopyToRadio.Enabled = CreateDirectoryFlag.Enabled = MoveToActionFlag.Checked;
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
			RenameToActionDateButton.Enabled = RenameToActionNumberButton.Enabled = RenameToActionNameButton.Enabled =
				RenameToActionField.Enabled = Label05.Enabled = Label10.Enabled = NumberOffsetField.Enabled =
				RenameToActionFlag.Checked;
			}

		// Добавление порядкового номера
		private void RenameToActionNumberButton_Click (object sender, EventArgs e)
			{
			string s = RDInterface.LocalizedMessageBox ("MessageLengthRequest", true, 1, "3");
			if (string.IsNullOrWhiteSpace (s))
				return;

			if (!"0123456789".Contains (s))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"MessageLengthError");
				return;
				}

			int idx = int.Parse (s);
			try
				{
				RenameToActionField.Text += renameMasks[idx];
				}
			catch { }

			RDInterface.SetFocusToTextbox (RenameToActionField);
			}

		// Добавление старого имени файла
		private void RenameToActionNameButton_Click (object sender, EventArgs e)
			{
			try
				{
				RenameToActionField.Text += renameMasks[11];
				}
			catch { }

			RDInterface.SetFocusToTextbox (RenameToActionField);
			}

		// Добавление даты
		private void RenameToActionDateButton_Click (object sender, EventArgs e)
			{
			try
				{
				RenameToActionField.Text += renameMasks[10];
				}
			catch { }

			RDInterface.SetFocusToTextbox (RenameToActionField);
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
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ErrorSourceDirectory");
				RDInterface.SetFocusToTextbox (SourceDirectoryField);
				return;
				}

			if (!Directory.Exists (SourceDirectoryField.Text))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ErrorSourceDirectoryUnavailable");
				RDInterface.SetFocusToTextbox (SourceDirectoryField);
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
					RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText |
						RDMessageFlags.LockSmallSize, "ErrorCharactersCriteria");
					RDInterface.SetFocusToTextbox (CharactersCriteriaField);
					return;
					}

				nameCriteria.AddRange (crt);
				}

			// Контроль фильтра по размеру файла
			if (SizeCriteriaFlag.Checked)
				{
				sizeComparisonMode = (ComparisonModes)SizeCriteriaCombo.SelectedIndex;
				sizeComparisonValue = (long)SizeCriteriaField.Value * (1L << (10 * SizeCriteriaUnitCombo.SelectedIndex));
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
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ErrorNoActions");
				return;
				}

			// Контроль конечной директории
			if (MoveToActionFlag.Checked)
				{
				if (string.IsNullOrWhiteSpace (MoveToActionField.Text))
					{
					RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"ErrorDestinationDirectory");
					RDInterface.SetFocusToTextbox (MoveToActionField);
					return;
					}

				bool created = true;
				if (!Directory.Exists (MoveToActionField.Text))
					{
					if (CreateDirectoryFlag.Checked)
						{
						// Попытка создания, если оно разрешено
						try
							{
							Directory.CreateDirectory (MoveToActionField.Text);
							}
						catch
							{
							created = false;
							}
						}
					else
						{
						// Создание запрещено – директория недоступна
						created = false;
						}
					}

				if (!created)
					{
					RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"ErrorDestinationDirectoryUnavailable");
					RDInterface.SetFocusToTextbox (MoveToActionField);
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
					RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText |
						RDMessageFlags.LockSmallSize, "ErrorNoSerialPattern");
					return;
					}
				}

			#endregion

			// Сбор сведений о файлах
			RDInterface.RunWork (CollectFileData, null, RDLocale.GetText ("MessageCollectingFileData"),
				RDRunWorkFlags.CaptionInTheMiddle);

			if (sourceFileNames.Count < 1)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText |
					RDMessageFlags.LockSmallSize, "ErrorNoFitsFound");
				return;
				}

			// Формирование имён конечных файлов
			startNumber = (ulong)NumberOffsetField.Value;
			RDInterface.RunWork (BuildFileNames, null, RDLocale.GetText ("MessageBuildingFileNames"),
				RDRunWorkFlags.CaptionInTheMiddle);

			// Выполнение операций
			RDInterface.RunWork (CopyMoveFiles, null, RDLocale.GetText ("MessageProcessingFiles"),
				RDRunWorkFlags.AllowOperationAbort | RDRunWorkFlags.CaptionInTheMiddle);
			switch (RDInterface.WorkResultAsInteger)
				{
				case -1:
					RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText | RDMessageFlags.LockSmallSize,
						string.Format (RDLocale.GetText ("ErrorFailedFileProcessing"), failedFileName));
					break;

				default:
					RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
						"MessageInterruption", 1500);
					break;

				case 0:
					RDInterface.LocalizedMessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
						"MessageSuccess", 1500);
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
			bool dateCriteria = (dateComparisonMode != ComparisonModes.Unused);
			bool sizeCriteria = (sizeComparisonMode != ComparisonModes.Unused);

			for (int i = 0; i < sourceFileNames.Count; i++)
				sourceFileInfo.Add (new FileInfo (sourceFileNames[i]));

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
					string.Format (RDLocale.GetText ("MessageProcessing"), i + 1, sourceFileNames.Count));

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

		// Метод формирования имён файлов
		private void BuildFileNames (object sender, DoWorkEventArgs e)
			{
			destinationFileNames.Clear ();
			for (int i = 0; i < sourceFileNames.Count; i++)
				{
				// Определение варианта имени файла
				string name;
				if (string.IsNullOrWhiteSpace (destinationName))
					name = Path.GetFileNameWithoutExtension (sourceFileNames[i]);
				else
					name = destinationName;

				// Расширение файла
				string ext = Path.GetExtension (sourceFileNames[i]);
				if (string.IsNullOrWhiteSpace (ext))
					ext = "";

				// Обработка подстановок
				for (int j = 0; j < destinationNameSubs.Count; j++)
					{
					SubstitutionTypes sType = (SubstitutionTypes)byte.Parse (destinationNameSubs[j][1]);

					switch (sType)
						{
						case SubstitutionTypes.SerialNumber:
						default:
							name = name.Replace (destinationNameSubs[j][0],
								/*(i + 1).ToString (destinationNameSubs[j][2]));*/
								((ulong)i + startNumber).ToString (destinationNameSubs[j][2]));
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

			e.Result = 0;
			}

		// Загрузка профиля
		private void ProfileLoadButton_Click (object sender, EventArgs e)
			{
			// Контроль
			NUProfile? prf = profilesSet.GetProfile ((uint)ProfileCombo.SelectedIndex);
			if (prf == null)
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText |
					RDMessageFlags.LockSmallSize, "ErrorCannotLoadProfile");
				return;
				}
			NUProfile profile = prf.Value;

			// Загрузка с защитой
			try
				{
				SourceDirectoryField.Text = profile.SourceDirectory;
				IncludeSubdirectoriesFlag.Checked = profile.IncludeSubdirectories;

				CharactersCriteriaFlag.Checked = profile.UseCharactersCriteria;
				CharactersCriteriaField.Text = profile.CharactersCriteria;

				SizeCriteriaFlag.Checked = profile.UseSizeCriteria;
				SizeCriteriaCombo.SelectedIndex = (byte)profile.SizeComparisonMode;
				SizeCriteriaField.Value = profile.SizeComparisonValue;
				SizeCriteriaUnitCombo.SelectedIndex = profile.SizeComparisonMultiplier;

				DateCriteriaFlag.Checked = profile.UseDateCriteria;
				DateCriteriaCombo.SelectedIndex = (byte)profile.DateComparisonMode;
				DateCriteriaField.Value = profile.DateComparisonValue;

				switch (profile.MoveToActionType)
					{
					case MoveToActions.None:
					default:
						MoveToActionFlag.Checked = false;
						break;

					case MoveToActions.Move:
						MoveToActionFlag.Checked = true;
						MoveToRadio.Checked = true;
						break;

					case MoveToActions.Copy:
						MoveToActionFlag.Checked = true;
						CopyToRadio.Checked = true;
						break;
					}

				MoveToActionField.Text = profile.DestinationDirectory;

				RenameToActionFlag.Checked = profile.UseRenameAction;
				RenameToActionField.Text = profile.RenamePattern;

				CreateDirectoryFlag.Checked = profile.CreateDirectory;
				NumberOffsetField.Value = profile.NumberOffset;
				}
			catch
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ErrorCannotLoadProfile");
				}
			}

		// Удаление профиля
		private void ProfileRemoveButton_Click (object sender, EventArgs e)
			{
			// Контроль
			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"MessageRemoveProfile", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne)
				return;

			// Удаление
			if (!profilesSet.RemoveProfile ((uint)ProfileCombo.SelectedIndex))
				return;

			ReloadProfiles ();
			}

		// Добавление профиля
		private void ProfileAddButton_Click (object sender, EventArgs e)
			{
			// Запрос имени профиля
			string name = RDInterface.LocalizedMessageBox ("MessageProfileName", true, 50);
			if (string.IsNullOrWhiteSpace (name))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"ErrorProfileNameIsEmpty", 1000);
				return;
				}

			// Сборка настроек
			NUProfile profile;
			profile.SourceDirectory = SourceDirectoryField.Text;
			profile.IncludeSubdirectories = IncludeSubdirectoriesFlag.Checked;

			profile.UseCharactersCriteria = CharactersCriteriaFlag.Checked;
			profile.CharactersCriteria = CharactersCriteriaField.Text;

			profile.UseSizeCriteria = SizeCriteriaFlag.Checked;
			profile.SizeComparisonMode = (ComparisonModes)SizeCriteriaCombo.SelectedIndex;
			profile.SizeComparisonValue = (ulong)SizeCriteriaField.Value;
			profile.SizeComparisonMultiplier = (byte)SizeCriteriaUnitCombo.SelectedIndex;

			profile.UseDateCriteria = DateCriteriaFlag.Checked;
			profile.DateComparisonMode = (ComparisonModes)DateCriteriaCombo.SelectedIndex;
			profile.DateComparisonValue = DateCriteriaField.Value;

			if (!MoveToActionFlag.Checked)
				profile.MoveToActionType = MoveToActions.None;
			else if (MoveToRadio.Checked)
				profile.MoveToActionType = MoveToActions.Move;
			else
				profile.MoveToActionType = MoveToActions.Copy;

			profile.DestinationDirectory = MoveToActionField.Text;

			profile.UseRenameAction = RenameToActionFlag.Checked;
			profile.RenamePattern = RenameToActionField.Text;

			profile.CreateDirectory = CreateDirectoryFlag.Checked;
			profile.NumberOffset = (ulong)NumberOffsetField.Value;

			// Добавление
			profile.Version = NUProfileVersions.Latest;
			if (!profilesSet.AddProfile (name, profile))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText |
					RDMessageFlags.LockSmallSize, "ErrorCannotSaveProfile");
				return;
				}

			// Успешно
			ReloadProfiles ();
			ProfileCombo.SelectedIndex = ProfileCombo.Items.Count - 1;
			}
		}
	}
