using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ACCPipes;
using Acer.CareCenter.ACCUIElements;
using Acer.CareCenter.ACCUtilities;
using Acer.CareCenter.Diagnostic;
using DiagnosticPlugin;
using Library;
using LogDebug;

namespace Acer.CareCenter.ACCDiagnosticPlg;

public class BatteryElementCtl(DiagnosticCg diagnosticCg) : CgElementCtl(diagnosticCg)
{
	private struct POWERBROADCAST_SETTING
	{
		public Guid PowerSetting;

		public uint DataLength;

		public byte Data;
	}

	private enum BatteryFeature
	{
		BatteryHealth,
		BatteryChargeLimit,
		BatteryCallibration
	}

	private bool SupportBatteryhealthControl;

	private PipeClient ACCUI_TO_ACCstd_PC;

	private PipeServer _pipeServer_checkSupportBatteryHealthControl;

	private Timer ResumeUpdateTimer;

	private const int CheckCalibrationStatus_TIME = 3000;

	private int Healthmode;

	private int Calibrationmode;

	private bool HealthStatus;

	private bool FirstOpenBatteryElement;

	private bool Launch_UI;

	private ReadWriteIni writeIni = new ReadWriteIni();

	private int GetBatteryCalibration_count;

	private object PoolObject = new object();

	private Guid GUID_CONSOLE_DISPLAY_STATE = new Guid("6fe69556-704a-47a0-8f24-c28d936fda47");

	private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;

	private bool SwitchIsEnabled = true;

	private bool RunningCalibration;

	private TestResult _CalibrationResult;

	private DateTime _CalibrationLastCheckTime;

	private string _on;

	private string _off;

	private string GUID_Criticalaction = "637ea02f-bbcb-4015-8e2c-a1c7b9c0b546";

	private string GUID_Critical_battery_level = "9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469";

	private string GUID_Reserve_battery_level = "f3c5027d-cd16-4930-aa6b-90db844a8f00";

	private string GUID_Hibernateafter = "9d7815a6-7ee4-497e-8888-515a05f02364";

	private string GUID_Sleepafter = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";

	private string GUID_Turnoffdisplayafter = "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e";

	private Guid subgroup_Battery = new Guid("e73a048d-bf27-4f12-9731-8b2076e8891f");

	private Guid subgroup_Sleepafter = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");

	private Guid subgroup_Displayafter = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");

	protected override void BaseInit()
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected Obj, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected Obj, but got Unknown
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected Obj, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected Obj, but got Unknown
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		LogPrefix = "BatteryElementCtl, ";
		_strDeviceType = DiagnosticManager.Instance.BATTERY_TYPE;
		if (!EnvironmentHelper._Instance.Get_IS_S_Mode_Result())
		{
			TransferData val = new TransferData();
			val.Command = (Commands)35;
			PipeClinetGetBatteryControlStatus(val);
		}
		_pipeServer_checkSupportBatteryHealthControl = new PipeServer((PipeServerType)4);
		_pipeServer_checkSupportBatteryHealthControl.Listen(PipeDef._ACCPipeName_ChecksupportHealthcontrol);
		_pipeServer_checkSupportBatteryHealthControl.PipeMessage_ACCstd_To_ACCUI += _pipeServer_PipeMessage;
		PresentationSource val2 = PresentationSource.FromVisual((Visual)(object)Application.Current.MainWindow);
		PresentationSource obj = ((val2 is HwndSource) ? val2 : null);
		((HwndSource)obj).AddHook((HwndSourceHook)WndProc);
		RegisterPowerSettingNotification(((HwndSource)obj).Handle, ref GUID_CONSOLE_DISPLAY_STATE, 0);
	}

	[DllImport("User32", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
	private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, int Flags);

	[DllImport("powrprof.dll")]
	private static extern bool IsPwrSuspendAllowed();

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 536)
		{
			if (wParam.ToInt32() == 32787 && ((POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING))).Data == 0 && !IsPwrSuspendAllowed())
			{
				Log.d("machine is modern standby and enter Sleep or Hibernation");
				CheckBatteryCalibrationstatus();
			}
			if (wParam.ToInt32() == 18)
			{
				Log.d("machine is not modern standby and enter Sleep or Hibernation");
				CheckBatteryCalibrationstatus();
			}
		}
		return IntPtr.Zero;
	}

	private void CheckBatteryCalibrationstatus()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Expected Obj, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		Log.d("CheckBatteryCalibrationstatus");
		Log.d("NowCalibrationStatus:" + NowCalibrationStatus);
		Log.d("HealthStatus:" + HealthStatus);
		if (NowCalibrationStatus)
		{
			GetBatteryCalibration_count = 0;
			Log.d("CalibrationStatus_count:" + GetBatteryCalibration_count);
			TransferData val = new TransferData();
			val.Command = (Commands)48;
			PipeClinetGetBatteryControlStatus(val);
			if (ResumeUpdateTimer == null)
			{
				Log.d("Create ResumeUpdateTimer");
				ResumeUpdateTimer = new Timer();
				ResumeUpdateTimer.Interval = 3000.0;
				ResumeUpdateTimer.Elapsed += GetCalibrationStatus;
				ResumeUpdateTimer.Enabled = true;
				ResumeUpdateTimer.Start();
			}
		}
	}

	private void PipeClinetGetBatteryControlStatus(TransferData DataForSend)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		new PipeClient().SendMsg(PipeDef._ACCPipeName, (object)DataForSend, 10000);
	}

	private void _pipeServer_PipeMessage(TransferData args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected I4, but got Unknown
		if ((int)args.Command != 0)
		{
			Commands command = args.Command;
			switch (command - 36)
			{
			case 0:
				CheckSupportBatteryHealthControl(args.argList);
				break;
			case 14:
				Log.d("GetBatteryCalibrationstatus");
				CheckBatteryCalibration_status(args.argList);
				break;
			case 1:
				Log.d("GetOpenBatteryHealthmode");
				GetOpenBatteryHealthmodeStatus(args.argList);
				break;
			case 2:
				Log.d("GetCloseBatteryHealthmode");
				GetCloseBatteryHealthmodeStatus(args.argList);
				break;
			case 7:
				Log.d("GetOpenBatteryCalibrationmode");
				GetOpenBatteryCalibrationStatus(args.argList);
				break;
			case 8:
				Log.d("GetCloseBatteryCalibrationmode:");
				GetCloseBatteryCalibrationStatus(args.argList);
				break;
			case 11:
				Log.d("GetAcStatus");
				GetAcStatusEvent(args.argList);
				break;
			case 12:
				Log.d("GetCalibrationEvent");
				GetCalibrationStatusEvent(args.argList);
				break;
			case 3:
			case 4:
			case 5:
			case 6:
			case 9:
			case 10:
			case 13:
				break;
			}
		}
	}

	private void CheckBatteryCalibration_status(List<object> args)
	{
		Log.d("CheckBatteryCalibration_status");
		bool flag = Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[4]);
		Log.d("CalibrationStatus " + flag);
		if (!flag)
		{
			NowCalibrationStatus = false;
			RunningCalibration = false;
			SwitchIsEnabled = true;
			Log.d("cancel calibration UI");
			Log.d("NowCalibrationStatus:" + NowCalibrationStatus);
			Log.d("HealthStatus:" + HealthStatus);
			((DispatcherObject)_pDiagnosticCgElement).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
			{
				((UIElement)_pDiagnosticCgElement.Switch).IsEnabled = true;
				((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)2;
				((Control)_pDiagnosticCgElement.RunCalibrationItemsControl).IsTabStop = false;
				if (battery_page == 3)
				{
					((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)0;
				}
			}), Array.Empty<object>());
			WriteCriticalBatteryLevel(Original_DC_CriticalBatteryAction, Original_DC_CriticalBatterylevel, Original_DC_ReserveBatterylevel, Original_AC_CriticalBatteryAction, Original_AC_CriticalBatterylevel, Original_AC_ReserveBatterylevel, Original_AC_Hibernateafter, Original_DC_Hibernateafter, Original_AC_Sleepafter, Original_DC_Sleepafter, Original_DC_TurnOffDisplayAfter, Original_AC_TurnOffDisplayAfter);
			Log.d("finish cancel calibration UI");
			CancelRegisterEvent("CancelCalibrationEvent");
			Delete_CriticalBatteryLevelini();
			Log.d("system enter Hibernation and cancel calibration UI");
			Cancel_Resumetimer();
		}
		else if (GetBatteryCalibration_count > 5)
		{
			Cancel_Resumetimer();
		}
	}

	private void Cancel_Resumetimer()
	{
		if (ResumeUpdateTimer != null)
		{
			Log.d("ResumeUpdateTimer: stop");
			ResumeUpdateTimer.Elapsed -= GetCalibrationStatus;
			ResumeUpdateTimer.Enabled = false;
			ResumeUpdateTimer.Stop();
			ResumeUpdateTimer = null;
			Log.d("ResumeUpdateTimer: stop");
		}
	}

	private void GetCalibrationStatus(object source, ElapsedEventArgs e)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected Obj, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		GetBatteryCalibration_count++;
		Log.d("CalibrationStatus_count:" + GetBatteryCalibration_count);
		TransferData val = new TransferData();
		val.Command = (Commands)48;
		PipeClinetGetBatteryControlStatus(val);
	}

	private void CheckSupportBatteryHealthControl(List<object> args)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		Log.d("CheckSupportBatteryHealthControl");
		List<string> list = args.Select((object s) => (string)s).ToList();
		string text = list[0];
		Healthmode = int.Parse(list[1]);
		Calibrationmode = int.Parse(list[2]);
		HealthStatus = Convert.ToBoolean(list[3]);
		WMIManagementWMI val = new WMIManagementWMI();
		double num = 0.0;
		val.WMIGetFunction_BIOSVersion(ref num);
		if (EnvironmentHelper._Instance.Get_IS_S_Mode_Result())
		{
			return;
		}
		Log.d("Launch_UI:" + Launch_UI);
		if (Launch_UI)
		{
			Log.d("HealthStatus:" + HealthStatus);
			if (!NowCalibrationStatus)
			{
				if (HealthStatus)
				{
					NowHealthmodeStatus = true;
				}
				else
				{
					NowHealthmodeStatus = false;
				}
			}
			((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
			{
				ChangeState(HealthStatus);
			}));
			if (!NowHealthmodeStatus && NowCalibrationStatus)
			{
				((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
				{
					ChangeState(state: false);
				}));
			}
		}
		else if (((object)HardwareDeviceManager.GetMachineType()/*cast due to constrained. prefix*/).ToString() == "Notebook" && Healthmode == 1 && Calibrationmode == 2 && text == "True" && num >= 2.58)
		{
			Log.d("SupportBatteryhealthControl");
			SupportBatteryhealthControl = true;
			FirstOpenBatteryElement = true;
			Launch_UI = true;
			Log.d("FirstOpenBatteryElement:" + FirstOpenBatteryElement);
		}
	}

	private void GetOpenBatteryHealthmodeStatus(List<object> args)
	{
		Log.d("GetOpenBatteryHealthmodeStatus");
		if (!Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[0]))
		{
			((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
			{
				ChangeState(state: false);
			}));
			NowHealthmodeStatus = false;
		}
		else
		{
			NowHealthmodeStatus = true;
		}
	}

	private void GetCloseBatteryHealthmodeStatus(List<object> args)
	{
		Log.d("GetCloseBatteryHealthmodeStatus");
		if (!Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[0]))
		{
			((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
			{
				ChangeState(state: true);
			}));
			NowHealthmodeStatus = true;
		}
		else
		{
			NowHealthmodeStatus = false;
		}
	}

	private void GetOpenBatteryCalibrationStatus(List<object> args)
	{
		Log.d("GetOpenBatteryCalibrationStatus");
		if (!Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[0]))
		{
			return;
		}
		ReadCriticalBatteryLevel();
		WriteCriticalBatteryLevel_ini();
		uint criticalBatteryAction = 0u;
		uint criticalBatterylevel = 1u;
		uint reservebatterylevel = 1u;
		uint dC_Hibernateafter = 0u;
		uint dC_Sleepafter = 0u;
		uint dC_TurnOffDisplayAfter = 0u;
		uint aC_CriticalBatteryAction = 0u;
		uint aC_CriticalBatterylevel = 1u;
		uint aC_Reservebatterylevel = 1u;
		uint aC_Hibernateafter = 0u;
		uint aC_Sleepafter = 0u;
		uint aC_TurnOffDisplayAfter = 0u;
		WriteCriticalBatteryLevel(criticalBatteryAction, criticalBatterylevel, reservebatterylevel, aC_CriticalBatteryAction, aC_CriticalBatterylevel, aC_Reservebatterylevel, aC_Hibernateafter, dC_Hibernateafter, aC_Sleepafter, dC_Sleepafter, dC_TurnOffDisplayAfter, aC_TurnOffDisplayAfter);
		NowCalibrationStatus = true;
		Log.d("NowCalibrationStatus:" + NowCalibrationStatus);
		Log.d("NowHealthmodeStatus:" + NowHealthmodeStatus);
		Log.d("HealthStatus:" + HealthStatus);
		if (NowHealthmodeStatus)
		{
			((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
			{
				ChangeState(state: false);
			}));
			NowHealthmodeStatus = false;
		}
		SwitchIsEnabled = false;
		Log.d("NowHealthmodeStatus:" + NowHealthmodeStatus);
		((DispatcherObject)_pDiagnosticCgElement).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
		{
			((UIElement)_pDiagnosticCgElement.Switch).IsEnabled = false;
		}), Array.Empty<object>());
	}

	private void WriteCriticalBatteryLevel_ini()
	{
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "DC_CriticalBatteryAction_value", Original_DC_CriticalBatteryAction.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "DC_CriticalBatterylevel_value", Original_DC_CriticalBatterylevel.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "DC_ReserveBatterylevel_value", Original_DC_ReserveBatterylevel.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "DC_Hibernateafter_value", Original_DC_Hibernateafter.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "DC_Sleepafter_value", Original_DC_Sleepafter.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "DC_TurnOffDisplayAfter_value", Original_DC_TurnOffDisplayAfter.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "AC_CriticalBatteryAction_value", Original_AC_CriticalBatteryAction.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "AC_CriticalBatterylevel_value", Original_AC_CriticalBatterylevel.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "AC_ReserveBatterylevel_value", Original_AC_ReserveBatterylevel.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "AC_Hibernateafter_value", Original_AC_Hibernateafter.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "AC_Sleepafter_value", Original_AC_Sleepafter.ToString());
		writeIni.WriteCriticalBatteryLevelInfoIni("BatteryPowersettings", "AC_TurnOffDisplayAfter_value", Original_AC_TurnOffDisplayAfter.ToString());
	}

	private void GetCloseBatteryCalibrationStatus(List<object> args)
	{
		Log.d("GetCloseBatteryCalibrationStatus");
		bool flag = Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[0]);
		Log.d("GetCloseBatteryCalibrationmode_errorcode:" + flag);
		if (!flag)
		{
			return;
		}
		Cancel_Resumetimer();
		Log.d("GetCloseBatteryCalibrationmode_errorcode: no error");
		SwitchIsEnabled = true;
		((DispatcherObject)_pDiagnosticCgElement).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
		{
			((UIElement)_pDiagnosticCgElement.Switch).IsEnabled = true;
			((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)2;
			((Control)_pDiagnosticCgElement.RunCalibrationItemsControl).IsTabStop = false;
			if (battery_page == 3)
			{
				((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)0;
			}
		}), Array.Empty<object>());
		WriteCriticalBatteryLevel(Original_DC_CriticalBatteryAction, Original_DC_CriticalBatterylevel, Original_DC_ReserveBatterylevel, Original_AC_CriticalBatteryAction, Original_AC_CriticalBatterylevel, Original_AC_ReserveBatterylevel, Original_AC_Hibernateafter, Original_DC_Hibernateafter, Original_AC_Sleepafter, Original_DC_Sleepafter, Original_DC_TurnOffDisplayAfter, Original_AC_TurnOffDisplayAfter);
		CancelRegisterEvent("CancelCalibrationEvent");
		NowCalibrationStatus = false;
		Log.d("NowCalibrationStatus:" + NowCalibrationStatus);
		Log.d("HealthStatus:" + HealthStatus);
		Log.d("NowHealthmodeStatus:" + NowHealthmodeStatus);
		Delete_CriticalBatteryLevelini();
	}

	private void Delete_CriticalBatteryLevelini()
	{
		if (File.Exists("C:\\ProgramData\\Acer\\CareCenter\\CriticalBatteryLevel.ini"))
		{
			try
			{
				File.Delete("C:\\ProgramData\\Acer\\CareCenter\\CriticalBatteryLevel.ini");
			}
			catch (IOException)
			{
			}
		}
	}

	private void GetAcStatusEvent(List<object> args)
	{
		if (!Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[0]))
		{
			Dialog.Box.Change_dgBatterycalibration_status("Hidden");
		}
		else
		{
			Dialog.Box.Change_dgBatterycalibration_status("Visible");
		}
	}

	private void GetCalibrationStatusEvent(List<object> args)
	{
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		Log.d("GetCalibrationStatusEvent");
		NowCalibrationStatus = false;
		Log.d("NowCalibrationStatus:" + NowCalibrationStatus);
		Log.d("HealthStatus:" + HealthStatus);
		RunningCalibration = false;
		WriteCriticalBatteryLevel(Original_DC_CriticalBatteryAction, Original_DC_CriticalBatterylevel, Original_DC_ReserveBatterylevel, Original_AC_CriticalBatteryAction, Original_AC_CriticalBatterylevel, Original_AC_ReserveBatterylevel, Original_AC_Hibernateafter, Original_DC_Hibernateafter, Original_AC_Sleepafter, Original_DC_Sleepafter, Original_DC_TurnOffDisplayAfter, Original_AC_TurnOffDisplayAfter);
		SwitchIsEnabled = true;
		((DispatcherObject)_pDiagnosticCgElement).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
		{
			((UIElement)_pDiagnosticCgElement.Switch).IsEnabled = true;
			((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)2;
			((Control)_pDiagnosticCgElement.RunCalibrationItemsControl).IsTabStop = false;
			if (battery_page == 3)
			{
				((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)0;
			}
		}), Array.Empty<object>());
		if (Convert.ToBoolean(args.Select((object s) => (string)s).ToList()[0]))
		{
			Log.d("BatteryCalibrations finish");
			_CalibrationLastCheckTime = DateTime.Now;
			_CalibrationResult = (TestResult)2;
			DiagnosticManager.Instance.RecordTestResult("BatteryCalibration", _CalibrationResult, _CalibrationLastCheckTime);
			UpdateCalibrationResult(_CalibrationResult, _CalibrationLastCheckTime);
		}
		Delete_CriticalBatteryLevelini();
		CancelRegisterEvent("CancelCalibrationEvent");
	}

	protected override void BuildCgElement()
	{
		_pDiagnosticCgElement.HWName.Text = (string)Application.Current.FindResource((object)"SystemCheckupBatteryHealth");
		((FrameworkElement)_pDiagnosticCgElement.HWName).ToolTip = _pDiagnosticCgElement.HWName.Text;
		Image hWImage = _pDiagnosticCgElement.HWImage;
		object obj = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"PageBatteryIcon");
		((FrameworkElement)hWImage).Style = (Style)((obj is Style) ? obj : null);
		((UIElement)_pDiagnosticCgElement.SuggestionItemsControl).Visibility = (Visibility)1;
		Button pageLeftBtn = _pDiagnosticCgElement.PageLeftBtn;
		object obj2 = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"LeftButton");
		((FrameworkElement)pageLeftBtn).Style = (Style)((obj2 is Style) ? obj2 : null);
		Button pageRightBtn = _pDiagnosticCgElement.PageRightBtn;
		object obj3 = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"RightButton");
		((FrameworkElement)pageRightBtn).Style = (Style)((obj3 is Style) ? obj3 : null);
		Button cancelButton = _pDiagnosticCgElement.CancelButton;
		object obj4 = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"CancelButton");
		((FrameworkElement)cancelButton).Style = (Style)((obj4 is Style) ? obj4 : null);
		Button calibrationCancelButton = _pDiagnosticCgElement.CalibrationCancelButton;
		object obj5 = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"CancelButton");
		((FrameworkElement)calibrationCancelButton).Style = (Style)((obj5 is Style) ? obj5 : null);
		_on = Application.Current.Resources[(object)"MUI_On"] as string;
		_off = Application.Current.Resources[(object)"MUI_Off"] as string;
		_pDiagnosticCgElement.Switch.Toggled += Switch_Toggled;
		if (EnvironmentHelper._Instance.Get_IS_S_Mode_Result())
		{
			((UIElement)_pDiagnosticCgElement.SuggestionItemsControl).Visibility = (Visibility)2;
		}
	}

	protected override int GetHwListCount(List<MultiDevicesTests> hwList)
	{
		int result = 0;
		if (SupportBatteryhealthControl)
		{
			result = Enum.GetNames(typeof(BatteryFeature)).Length;
		}
		else if (hwList != null && hwList.Count > 0)
		{
			result = hwList[0].m_lDeviceList.Count;
		}
		return result;
	}

	protected override void DiagDeviceInfoUpdate()
	{
		_lHWList = DiagnosticManager.Instance.GetSpecificDeviceInfo(DeviceType.BatteryManager);
	}

	protected override void CgElementUpdate(List<MultiDevicesTests> hwList, int index, ref string serialNumber)
	{
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Expected Obj, but got Unknown
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
		_ = hwList[0].m_lDeviceList;
		_pDiagnosticCgElement.DetailInfoContent.Items.Clear();
		switch (index)
		{
		case 0:
		{
			battery_page = 1;
			Image hWImage2 = _pDiagnosticCgElement.HWImage;
			object obj2 = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"PageBatteryIcon");
			((FrameworkElement)hWImage2).Style = (Style)((obj2 is Style) ? obj2 : null);
			_pDiagnosticCgElement.HWName.Text = (string)Application.Current.FindResource((object)"SystemCheckupBatteryHealth");
			((FrameworkElement)_pDiagnosticCgElement.HWName).ToolTip = _pDiagnosticCgElement.HWName.Text;
			((UIElement)_pDiagnosticCgElement.TestStart).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.HWResult).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.Status).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.OtherMg).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.BatteryCalibration).Visibility = (Visibility)2;
			((DependencyObject)_pDiagnosticCgElement.HWImage).SetValue(AutomationProperties.NameProperty, (object)_pDiagnosticCgElement.HWName.Text);
			((FrameworkElement)_pDiagnosticCgElement.NormalStatus).Height = 67.0;
			((UIElement)_pDiagnosticCgElement.BatteryChargeLimit).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)2;
			((Control)_pDiagnosticCgElement.RunCalibrationItemsControl).IsTabStop = false;
			((UIElement)_pDiagnosticCgElement.Checking).Visibility = (Visibility)0;
			_pProgressBarSmoother.Visible();
			((UIElement)_pDiagnosticCgElement.CancelButton).Visibility = (Visibility)0;
			((Control)_pDiagnosticCgElement.BatteryLimitOffItemsControl).IsTabStop = false;
			((Control)_pDiagnosticCgElement.CalibrationCheckItemsControl).IsTabStop = false;
			break;
		}
		case 1:
		{
			TransferData val = new TransferData();
			val.Command = (Commands)35;
			PipeClinetGetBatteryControlStatus(val);
			if (FirstOpenBatteryElement)
			{
				if (HealthStatus)
				{
					NowHealthmodeStatus = true;
				}
				else
				{
					NowHealthmodeStatus = false;
				}
				ChangeState(HealthStatus);
				FirstOpenBatteryElement = false;
			}
			battery_page = 2;
			Image hWImage3 = _pDiagnosticCgElement.HWImage;
			object obj3 = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"PageBatteryLimitIcon");
			((FrameworkElement)hWImage3).Style = (Style)((obj3 is Style) ? obj3 : null);
			((FrameworkElement)_pDiagnosticCgElement.NormalStatus).Height = 105.0;
			_pDiagnosticCgElement.HWName.Text = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryChargeLimit");
			((FrameworkElement)_pDiagnosticCgElement.HWName).ToolTip = _pDiagnosticCgElement.HWName.Text;
			((UIElement)_pDiagnosticCgElement.TestStart).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.HWResult).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.Status).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.BatteryCalibration).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.OtherMg).Visibility = (Visibility)2;
			_pDiagnosticCgElement.BatteryLimitDescription.Text = (string)Application.Current.FindResource((object)"MUI_ChargeLimit");
			((UIElement)_pDiagnosticCgElement.BatteryChargeLimit).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)2;
			((Control)_pDiagnosticCgElement.RunCalibrationItemsControl).IsTabStop = false;
			((UIElement)_pDiagnosticCgElement.Checking).Visibility = (Visibility)2;
			_pProgressBarSmoother.Visible_Collapsed();
			((UIElement)_pDiagnosticCgElement.CancelButton).Visibility = (Visibility)2;
			if (SwitchIsEnabled)
			{
				((UIElement)_pDiagnosticCgElement.Switch).IsEnabled = true;
			}
			else
			{
				((UIElement)_pDiagnosticCgElement.Switch).IsEnabled = false;
			}
			break;
		}
		case 2:
		{
			battery_page = 3;
			((FrameworkElement)_pDiagnosticCgElement.NormalStatus).Height = 80.0;
			Image hWImage = _pDiagnosticCgElement.HWImage;
			object obj = ((FrameworkElement)_pDiagnosticCg).FindResource((object)"PageBatteryCalibrateIcon");
			((FrameworkElement)hWImage).Style = (Style)((obj is Style) ? obj : null);
			_pDiagnosticCgElement.HWName.Text = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration");
			((FrameworkElement)_pDiagnosticCgElement.HWName).ToolTip = _pDiagnosticCgElement.HWName.Text;
			((UIElement)_pDiagnosticCgElement.OtherMg).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.TestStart).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.BatteryChargeLimit).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.BatteryCalibration).Visibility = (Visibility)0;
			_pDiagnosticCgElement.BatteryCalibrationDescription.Text = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibrationInfo");
			((Control)_pDiagnosticCgElement.CalibrationCheckItemsControl).IsTabStop = true;
			((UIElement)_pDiagnosticCgElement.CalibrationCheckItemsControl).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.CalibrationItemsControl).Visibility = (Visibility)0;
			_CalibrationResult = DiagnosticManager.Instance.GetRecordResult("BatteryCalibration", serialNumber);
			_CalibrationLastCheckTime = DiagnosticManager.Instance.GetRecordTime("BatteryCalibration", serialNumber);
			UpdateCalibrationResult(_CalibrationResult, _CalibrationLastCheckTime);
			if (RunningCalibration)
			{
				((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)2;
				((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)0;
				((UIElement)_pDiagnosticCgElement.CalibrationCancelItemsControl).Visibility = (Visibility)0;
			}
			break;
		}
		}
		if (getBatteryLaunchByCortana())
		{
			TestStart_Click(this, null);
			setBatteryLaunchByCortana(bLaunchByCortana: false);
		}
	}

	private byte CheckACStatus()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		SYSTEM_POWER_STATUS val = new SYSTEM_POWER_STATUS();
		Win32Wrapper.GetSystemPowerStatus(val);
		return val.ACLineStatus;
	}

	private void ReadCriticalBatteryLevel()
	{
		Guid guid = new Guid(GUID_Criticalaction);
		Guid guid2 = new Guid(GUID_Critical_battery_level);
		Guid guid3 = new Guid(GUID_Reserve_battery_level);
		Guid guid4 = new Guid(GUID_Hibernateafter);
		Guid guid5 = new Guid(GUID_Sleepafter);
		Guid guid6 = new Guid(GUID_Turnoffdisplayafter);
		IntPtr intPtr = default;
		NativeMethods.PowerGetActiveScheme(IntPtr.Zero, ref intPtr);
		Guid guid7 = (Guid)Marshal.PtrToStructure(intPtr, typeof(Guid));
		NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid, ref Original_DC_CriticalBatteryAction);
		NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid2, ref Original_DC_CriticalBatterylevel);
		NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid3, ref Original_DC_ReserveBatterylevel);
		NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid4, ref Original_DC_Hibernateafter);
		NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid5, ref Original_DC_Sleepafter);
		NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Displayafter, ref guid6, ref Original_DC_TurnOffDisplayAfter);
		NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid, ref Original_AC_CriticalBatteryAction);
		NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid2, ref Original_AC_CriticalBatterylevel);
		NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid3, ref Original_AC_ReserveBatterylevel);
		NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid4, ref Original_AC_Hibernateafter);
		NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid5, ref Original_AC_Sleepafter);
		NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Displayafter, ref guid6, ref Original_AC_TurnOffDisplayAfter);
		Marshal.FreeHGlobal(intPtr);
	}

	private void WriteCriticalBatteryLevel(uint CriticalBatteryAction, uint CriticalBatterylevel, uint Reservebatterylevel, uint AC_CriticalBatteryAction, uint AC_CriticalBatterylevel, uint AC_Reservebatterylevel, uint AC_Hibernateafter, uint DC_Hibernateafter, uint AC_Sleepafter, uint DC_Sleepafter, uint DC_TurnOffDisplayAfter, uint AC_TurnOffDisplayAfter)
	{
		Guid guid = new Guid(GUID_Criticalaction);
		Guid guid2 = new Guid(GUID_Critical_battery_level);
		Guid guid3 = new Guid(GUID_Reserve_battery_level);
		Guid guid4 = new Guid(GUID_Hibernateafter);
		Guid guid5 = new Guid(GUID_Sleepafter);
		Guid guid6 = new Guid(GUID_Turnoffdisplayafter);
		IntPtr intPtr = default;
		NativeMethods.PowerGetActiveScheme(IntPtr.Zero, ref intPtr);
		Guid guid7 = (Guid)Marshal.PtrToStructure(intPtr, typeof(Guid));
		NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid, CriticalBatteryAction);
		NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid2, CriticalBatterylevel);
		NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid3, Reservebatterylevel);
		NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid4, DC_Hibernateafter);
		NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid5, DC_Sleepafter);
		NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Displayafter, ref guid6, DC_TurnOffDisplayAfter);
		NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid, AC_CriticalBatteryAction);
		NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid2, AC_CriticalBatterylevel);
		NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Battery, ref guid3, AC_Reservebatterylevel);
		NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid4, AC_Hibernateafter);
		NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Sleepafter, ref guid5, AC_Sleepafter);
		NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid7, ref subgroup_Displayafter, ref guid6, AC_TurnOffDisplayAfter);
		NativeMethods.PowerSetActiveScheme(IntPtr.Zero, ref guid7);
		Marshal.FreeHGlobal(intPtr);
	}

	private void Switch_Toggled(object sender, ToggleEventArgs e)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected Obj, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected Obj, but got Unknown
		Log.d("Switch_Toggled");
		if (CheckEnvironment.Instance.CheckModelTypeStr() != "Consumer")
		{
			BrushConverter val = new BrushConverter();
			_pDiagnosticCgElement.BatteryLimitOff.Foreground = (Brush)((TypeConverter)(object)val).ConvertFromString("#808080");
		}
		Log.d("NowCalibrationStatus:" + NowCalibrationStatus);
		Log.d("NowHealthmodeStatus:" + NowHealthmodeStatus);
		Log.d("HealthStatus:" + HealthStatus);
		if (e.State == SwitchButton.SwitchButtonState.On)
		{
			Log.d("SwitchButton.SwitchButtonState.On");
			_pDiagnosticCgElement.BatteryLimitOff.Text = _on;
			if (!NowHealthmodeStatus && !NowCalibrationStatus)
			{
				ChargeLimit_warmingMessage();
			}
			if (!NowHealthmodeStatus && NowCalibrationStatus)
			{
				((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
				{
					ChangeState(state: false);
				}));
			}
		}
		else
		{
			Log.d("SwitchButton.SwitchButtonState.Off");
			_pDiagnosticCgElement.BatteryLimitOff.Text = _off;
			if (NowHealthmodeStatus && !NowCalibrationStatus)
			{
				CloseBatteryHealthmode();
			}
		}
	}

	private void StartBatteryHealthmode()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		Log.d("StartBatteryHealthmode");
		TransferData val = new TransferData();
		val.Command = (Commands)39;
		PipeClinetGetBatteryControlStatus(val);
	}

	private void StartBatteryCalibrationmode()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		Log.d("SetOpenBatteryCalibarationmode:");
		TransferData val = new TransferData();
		val.Command = (Commands)42;
		PipeClinetGetBatteryControlStatus(val);
	}

	private void CloseBatteryCalibrationmode()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		Log.d("SetCloseBatteryCalibrationmode:");
		TransferData val = new TransferData();
		val.Command = (Commands)41;
		PipeClinetGetBatteryControlStatus(val);
	}

	private void CloseBatteryHealthmode()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		Log.d("SetCloseBatteryHealthmode");
		TransferData val = new TransferData();
		val.Command = (Commands)40;
		PipeClinetGetBatteryControlStatus(val);
	}

	private void RegisterEvent(string Event)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		TransferData val = new TransferData();
		val.Command = (Commands)45;
		List<object> list = new List<object>();
		list.Add(Event);
		val.argList = list;
		PipeClinetGetBatteryControlStatus(val);
	}

	public void ChangeState(bool state)
	{
		if (state)
		{
			_pDiagnosticCgElement.BatteryLimitOff.Text = _on;
			_pDiagnosticCgElement.Switch.State = SwitchButton.SwitchButtonState.On;
		}
		else
		{
			_pDiagnosticCgElement.BatteryLimitOff.Text = _off;
			_pDiagnosticCgElement.Switch.State = SwitchButton.SwitchButtonState.Off;
		}
	}

	private void UpdateCalibrationResult(TestResult result, DateTime dateTime)
	{
		string lastTime;
		if (dateTime == DateTime.MinValue)
		{
			lastTime = "None";
		}
		else
		{
			lastTime = ((dateTime.Date == DateTime.Today.Date) ? ((string)Application.Current.FindResource((object)"Checkup_Today")) : dateTime.ToShortDateString());
		}
		((DispatcherObject)_pDiagnosticCgElement).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
		{
			if (dateTime == DateTime.MinValue)
			{
				_pDiagnosticCgElement.CalibrationCheck.Text = (string)Application.Current.FindResource((object)"SystemCheckupLastCheckedNone");
			}
			else
			{
				_pDiagnosticCgElement.CalibrationCheck.Text = (string)Application.Current.FindResource((object)"SystemCheckupLastCheck") + lastTime;
			}
		}), Array.Empty<object>());
	}

	private void ChargeLimit_warmingMessage()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected Obj, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected Obj, but got Unknown
		string text = (string)Application.Current.FindResource((object)"MUI_ChargeLimit_Warning_Title");
		string text2 = (string)Application.Current.FindResource((object)"MUI_Warning_ChargeLimit_message");
		TextBlock val = new TextBlock
		{
			TextWrapping = (TextWrapping)2
		};
		_ = (Color)ColorConverter.ConvertFromString("#FF3030");
		val.Inlines.Add(text2);
		Dialog.Box.New_DialogType_Batterycalibration();
		HowItWorkWindowUserControl howItWorkWindowUserControl = new HowItWorkWindowUserControl(val, string.Empty);
		arg val2 = new arg(text, (UserControl)(object)howItWorkWindowUserControl, (string)null, (string)Application.Current.FindResource((object)"MUI_Cancel"), (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_Continue"));
		switch (Dialog.Box._Show((Type)10, (object)val2))
		{
		case 2:
			StartBatteryHealthmode();
			break;
		case 1:
		case 3:
			((DispatcherObject)_pDiagnosticCgElement.Switch).Dispatcher.BeginInvoke((DispatcherPriority)9, (Delegate)(Action)(() =>
			{
				ChangeState(state: false);
			}));
			break;
		}
	}

	protected override void RunCalibration()
	{
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Expected Obj, but got Unknown
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Expected Obj, but got Unknown
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Expected Obj, but got Unknown
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Expected Obj, but got Unknown
		//IL_02bb: Expected Obj, but got Unknown
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Expected Obj, but got Unknown
		//IL_02da: Expected Obj, but got Unknown
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Expected Obj, but got Unknown
		//IL_02f9: Expected Obj, but got Unknown
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0313: Expected Obj, but got Unknown
		//IL_0318: Expected Obj, but got Unknown
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Expected Obj, but got Unknown
		//IL_0337: Expected Obj, but got Unknown
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Expected Obj, but got Unknown
		//IL_0356: Expected Obj, but got Unknown
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Expected Obj, but got Unknown
		//IL_0375: Expected Obj, but got Unknown
		//IL_0389: Expected Obj, but got Unknown
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Expected Obj, but got Unknown
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Expected Obj, but got Unknown
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Expected Obj, but got Unknown
		string text = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration");
		string text2 = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibrationDescription") + "\n\n";
		string text3 = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibrationStep") + ".1\n";
		string text4 = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_StepDesc_1") + "\n\n";
		string text5 = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibrationStep") + ".2\n";
		string text6 = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_StepDesc_2") + "\n\n";
		string text7 = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibrationStep") + ".3\n";
		string text8 = "*" + (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_StepDesc_3") + "\n\n";
		string text9 = "*" + (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_Note_1") + "\n\n";
		string text10 = "*" + (string)Application.Current.FindResource((object)"MUI_CalibrationNote1") + "\n";
		string text11 = "  -" + (string)Application.Current.FindResource((object)"MUI_CalibrationNote2") + "\n";
		string text12 = "  -" + (string)Application.Current.FindResource((object)"MUI_CalibrationNote3") + "\n";
		string text13 = "  -" + (string)Application.Current.FindResource((object)"MUI_CalibrationNote4") + "\n\n";
		string text14 = "*" + (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_Note_3") + "\n\n";
		string text15 = "*" + (string)Application.Current.FindResource((object)"MUI_CalibrationNote5") + "\n\n";
		TextBlock val = new TextBlock
		{
			TextWrapping = (TextWrapping)2
		};
		Color val2 = (Color)ColorConverter.ConvertFromString("#FF3030");
		val.Inlines.Add(text2);
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text3)
		{
			FontWeight = FontWeights.Bold
		});
		val.Inlines.Add(text4);
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text5)
		{
			FontWeight = FontWeights.Bold
		});
		val.Inlines.Add(text6);
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text7)
		{
			FontWeight = FontWeights.Bold
		});
		val.Inlines.Add(text8);
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text9)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text10)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text11)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text12)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text13)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text14)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		((TextElementCollection<Inline>)(object)val.Inlines).Add((Inline)new Run(text15)
		{
			Foreground = (Brush)new SolidColorBrush(val2)
		});
		Dialog.Box.New_DialogType_Batterycalibration();
		HowItWorkWindowUserControl howItWorkWindowUserControl = new HowItWorkWindowUserControl(val, string.Empty);
		if (CheckACStatus() == 0)
		{
			Dialog.Box.Change_dgBatterycalibration_status("Hidden");
		}
		arg val3 = new arg(text, (UserControl)(object)howItWorkWindowUserControl, (string)null, (string)Application.Current.FindResource((object)"MUI_Cancel"), (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_Continue"));
		RegisterEvent("ACEvent");
		switch (Dialog.Box._Show((Type)10, (object)val3))
		{
		case 2:
			RunningCalibration = true;
			((UIElement)_pDiagnosticCgElement.CalibrationButton).Visibility = (Visibility)2;
			((UIElement)_pDiagnosticCgElement.RunCalibration).Visibility = (Visibility)0;
			((UIElement)_pDiagnosticCgElement.CalibrationCancelItemsControl).Visibility = (Visibility)0;
			if (CheckEnvironment.Instance.CheckModelTypeStr() != "Consumer")
			{
				BrushConverter val4 = new BrushConverter();
				_pDiagnosticCgElement.Calibrating.Foreground = (Brush)((TypeConverter)(object)val4).ConvertFromString("#808080");
			}
			_pDiagnosticCgElement.Calibrating.Text = (string)Application.Current.FindResource((object)"MUI_Battery_BatteryCalibration_Calibrating");
			CancelRegisterEvent("CancelACEvent");
			RegisterEvent("CalibrationEvent");
			StartBatteryCalibrationmode();
			break;
		case 1:
		case 3:
			CancelRegisterEvent("CancelACEvent");
			break;
		}
	}

	private void CancelRegisterEvent(string Event)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected Obj, but got Unknown
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		Log.d("CancelRegisterEvent " + Event);
		TransferData val = new TransferData();
		val.Command = (Commands)46;
		List<object> list = new List<object>();
		list.Add(Event);
		val.argList = list;
		PipeClinetGetBatteryControlStatus(val);
	}

	protected override void CancelCalibration()
	{
		Log.d("press CancelCalibration");
		RunningCalibration = false;
		CloseBatteryCalibrationmode();
	}

	protected override void UpdateTestResult(TestResult result, DateTime dateTime)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected Obj, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected I4, but got Unknown
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Expected Obj, but got Unknown
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Expected Obj, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected Obj, but got Unknown
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected Obj, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected Obj, but got Unknown
		string text;
		if (dateTime == DateTime.MinValue)
		{
			text = "None";
		}
		else
		{
			text = ((dateTime.Date == DateTime.Today.Date) ? ((string)Application.Current.FindResource((object)"Checkup_Today")) : dateTime.ToShortDateString());
		}
		BrushConverter val = new BrushConverter();
		switch ((int)result)
		{
		default:
			return;
		case 2:
			if (CheckEnvironment.Instance.CheckModelTypeStr() == "Consumer")
			{
				_pDiagnosticCgElement.HWResult.Foreground = (Brush)((TypeConverter)(object)val).ConvertFromString("#FF83B81A");
			}
			else
			{
				_pDiagnosticCgElement.HWResult.Foreground = (Brush)((TypeConverter)(object)val).ConvertFromString("#FF9BD71E");
			}
			_pDiagnosticCgElement.HWResult.Text = (string)Application.Current.FindResource((object)"SystemCheckupGood");
			((UIElement)_pDiagnosticCgElement.SuggestionItemsControl).Visibility = (Visibility)2;
			break;
		case 3:
		case 6:
			_pDiagnosticCgElement.HWResult.Foreground = (Brush)((TypeConverter)(object)val).ConvertFromString("#FFFCA000");
			_pDiagnosticCgElement.HWResult.Text = (string)Application.Current.FindResource((object)"SystemCheckupNeedAttention");
			((UIElement)_pDiagnosticCgElement.SuggestionItemsControl).Visibility = (Visibility)0;
			break;
		case 0:
			if (CheckEnvironment.Instance.CheckModelTypeStr() == "Consumer")
			{
				_pDiagnosticCgElement.HWResult.Foreground = (Brush)((TypeConverter)(object)val).ConvertFromString("#FF83B81A");
			}
			else
			{
				_pDiagnosticCgElement.HWResult.Foreground = (Brush)((TypeConverter)(object)val).ConvertFromString("#FF9BD71E");
			}
			_pDiagnosticCgElement.HWResult.Text = (string)Application.Current.FindResource((object)"SystemCheckupNoCheck");
			((UIElement)_pDiagnosticCgElement.SuggestionItemsControl).Visibility = (Visibility)2;
			break;
		case 1:
		case 4:
		case 5:
			return;
		}
		if (dateTime == DateTime.MinValue)
		{
			_pDiagnosticCgElement.OtherMg.Text = (string)Application.Current.FindResource((object)"SystemCheckupLastCheckedNone");
		}
		else
		{
			_pDiagnosticCgElement.OtherMg.Text = (string)Application.Current.FindResource((object)"SystemCheckupLastCheck") + text;
		}
	}

	protected override void RunTest()
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected Obj, but got Unknown
		((UIElement)_pDiagnosticCgElement.TestProgressBarItemsControl).Visibility = (Visibility)0;
		((Control)_pDiagnosticCgElement.TestProgressBarItemsControl).IsTabStop = true;
		((UIElement)_pDiagnosticCgElement.TestProgressItemsControl).Visibility = (Visibility)0;
		((UIElement)_pDiagnosticCgElement.CheckingItemsControl).Visibility = (Visibility)0;
		((Control)_pDiagnosticCgElement.CheckingItemsControl).IsTabStop = true;
		List<MultiDevicesTests> list = new List<MultiDevicesTests>();
		List<DeviceTests> list2 = new List<DeviceTests>();
		list2.Add(_lHWList[0].m_lDeviceList[_iHWIndex]);
		list.Add(new MultiDevicesTests(list2, _lHWList[0].m_strDeviceType, false));
		if (!DiagnosticManager.Instance.StartTest(list))
		{
			Log.d(LogPrefix + "TestStart_Click: Hard disk or Network test is running! Just return.");
			CgElementUpdate((TestResult)1, 0f);
			_pDiagnosticCgElement.TestProgress.Text = (string)Application.Current.FindResource((object)"SystemCheckupWaitting");
		}
		GA.Inst.ga_name.SendData((ActionName)13, (ActionValue)92, (string)null);
	}

	protected override void SuggestionMessageUpdate()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected Obj, but got Unknown
		arg val = new arg((string)Application.Current.FindResource((object)"SystemCheckupSuggest"), (string)Application.Current.FindResource((object)"Checkup_BttSg"), (string)Application.Current.FindResource((object)"MUI_OK"));
		Dialog.Box._Show((Type)7, (object)val);
	}
}
