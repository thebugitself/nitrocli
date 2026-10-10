using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using ACCPipes;
using ACCStd;
using Acer.CareCenter.ACCEvents;
using Acer.CareCenter.ACCNotifyShow;
using Acer.CareCenter.ACCTuneUpPlg;
using Acer.CareCenter.ACCUIElements;
using Acer.CareCenter.ACCUtilities;
using Acer.CareCenter.Diagnostic;
using Acer.CareCenter.LiveUpdate;
using Acer.CareCenter.Plugin;
using CommLib;
using Library;
using LogDebug;
using Microsoft.Win32;
using MonitorControlLib;
using Monitor_daNetLib4;
using TsDotNetLib;
using Windows.Foundation.Metadata;

namespace Acer.CareCenter.ACCStd;

public partial class MainWindow : Window, IComponentConnector
{
	public enum DriveType
	{
		HDD,
		SSD,
		EMMC,
		UnKnown,
		NvME,
		OPTANE,
		OPTANE_SSD,
		UFS
	}

	public class HDDDeviceInfo
	{
		public string _HDDName = "";

		public DriveType _DeviceType = DriveType.UnKnown;

		public string _Partition = "";

		public string _HDDSN = "";

		public HDDDeviceInfo(string hDDName, string partition, DriveType deviceType, string SN)
		{
			_HDDName = hDDName;
			_DeviceType = deviceType;
			_Partition = partition;
			_HDDSN = SN;
		}
	}

	private struct parameterBatteryhealthControl
	{
		public uint output;

		public int SupportQuery;

		public int HealthMode;

		public int CalibrationMode;

		public bool State_CalibrationMode;

		public bool State_HealthMode;

		public void intialize(uint a1, int a2, int a3, int a4, bool a5, bool a6)
		{
			output = a1;
			SupportQuery = a2;
			HealthMode = a3;
			CalibrationMode = a4;
			State_CalibrationMode = a5;
			State_HealthMode = a6;
		}
	}

	public struct HardDriveName(string Name, string Type, string Media, string index, string sn)
	{
		public string HardDriveType = Type;

		public string HardModelName = Name;

		public string MediaType = Media;

		public string Index = index;

		public string SN = sn;
	}

	public enum ShowCommands
	{
		SW_HIDE = 0,
		SW_SHOWNORMAL = 1,
		SW_NORMAL = SW_SHOWNORMAL,
		SW_SHOWMINIMIZED = 2,
		SW_SHOWMAXIMIZED = 3,
		SW_MAXIMIZE = SW_SHOWMAXIMIZED,
		SW_SHOWNOACTIVATE = 4,
		SW_SHOW = 5,
		SW_MINIMIZE = 6,
		SW_SHOWMINNOACTIVE = 7,
		SW_SHOWNA = 8,
		SW_RESTORE = 9,
		SW_SHOWDEFAULT = 10,
		SW_FORCEMINIMIZE = 11,
		SW_MAX = SW_FORCEMINIMIZE
	}

	public enum ActivateOptions
	{
		None = 0,
		DesignMode = 1,
		NoErrorUI = 2,
		NoSplashScreen = 4
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
	private interface IApplicationActivationManager
	{
		IntPtr ActivateApplication([In] string appUserModelId, [In] string arguments, [In] ActivateOptions options, out uint processId);

		IntPtr ActivateForFile([In] string appUserModelId, [In][MarshalAs(UnmanagedType.Interface)] IShellItemArray itemArray, [In] string verb, out uint processId);

		IntPtr ActivateForProtocol([In] string appUserModelId, [In] IntPtr itemArray, out uint processId);
	}

	[ComImport]
	[Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
	private class ApplicationActivationManager : IApplicationActivationManager
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern IntPtr ActivateApplication([In] string appUserModelId, [In] string arguments, [In] ActivateOptions options, out uint processId);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern IntPtr ActivateForFile([In] string appUserModelId, [In][MarshalAs(UnmanagedType.Interface)] IShellItemArray itemArray, [In] string verb, out uint processId);

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern IntPtr ActivateForProtocol([In] string appUserModelId, [In] IntPtr itemArray, out uint processId);
	}

	[ComImport]
	[Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItemArray
	{
	}

	private enum SelfUpdate_ExitValue
	{
		NO_NetWork = 1124,
		NO_NewVersion
	}

	private const string INTANCEMUTEXGUID = "{9D8FBB56-3126-46E0-8248-6DE7CBBE8A60}";

	private const string adapterKeysPath = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E972-E325-11CE-BFC1-08002bE10318}";

	private const string HDD_SMART_SELF_TEST = "SMART SelfTest";

	private const string HDD_SMART_STATUS_TEST = "SMART Status Test";

	private const string HDD_SMART_THRESHOLD_TEST = "SMART Threshold Test";

	private const string HDD_RANDOM_SEEK_TEST = "Random Seek Test";

	private const string HDD_SMART_WEAR_LEVEL_TEST = "SMART Wear Level Test";

	private const string NETWORK_CONNECTIVITY_CHECK = "Connectivity Check";

	private const string NETWORK_IPADDRESS_CHECK = "IP Address Check";

	private const string NETWORK_PING_TEST = "Ping Function Test";

	private const int NETWORK_CHANGED_MONITOR_TIME = 2500;

	private static SingleInstance singleInstance;

	private bool isFirstLaunch_HardDriveUpdate = true;

	private static string LOCALAPPDATA = Environment.GetEnvironmentVariable("LocalAppData");

	private static string PACKAGES = Path.Combine(LOCALAPPDATA, "Packages");

	private FileSystemWatcher fsw = new FileSystemWatcher();

	private string _pluginFolderName = "ACCMonitorPlugin";

	private PipeServer _pipeServer;

	private string _mainProgramNameACC;

	private string _mainProgramNameACtC;

	private string _appName;

	private string _model;

	private string _brand;

	private readonly string ACC_UWP_Name = "CareCenter";

	private readonly string ACtC_UWP_Name = "ControlCenter";

	private List<MultiDevicesTests> _lHDDList;

	private List<MultiDevicesTests> _lNetworkList;

	private List<MultiDevicesTests> _lBatteryList;

	private List<HDDDeviceInfo> _currentDriveList;

	private List<HDDDeviceInfo> _currentDriveList_detail;

	private System.Threading.Timer _timeForDriveChange;

	private bool _delayTriggerChange;

	private readonly int _delayTime = 60000;

	private string DST_ProgressIni = "DST_Progress.ini";

	private string DST_ResultIni = "DST_Result.ini";

	private string NVMEDST_tool = "AccNvmeDST.exe";

	private bool Cancel_NVMEDST;

	private ManagementEventWatcher ACEventwmiWatcher;

	private ManagementEventWatcher CalibrationEventwmiWatcher;

	private bool Monitor_ADS;

	private bool getHDDInformation;

	private System.Threading.Timer _timeForSelfupgradeCheck;

	private ManagementScope bar;

	private ManagementObjectSearcher queryPowerManagement;

	private ManagementObjectSearcher queryWake;

	private WMIManagementWMI wmimanagementwmi;

	private static object _pHardwareDeviceLock = new object();

	private static object _pProcLock = new object();

	public static byte[] WMIEvent;

	private Guid subgroup_battery = new Guid("e73a048d-bf27-4f12-9731-8b2076e8891f");

	private Guid subgroup_Sleep = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");

	private Guid subgroup_Displayafter = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");

	private string GUID_Critical_battery_level = "9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469";

	private string GUID_Reservebatterylevel = "f3c5027d-cd16-4930-aa6b-90db844a8f00";

	private string GUID_Hibernate_after = "9d7815a6-7ee4-497e-8888-515a05f02364";

	private string GUID_Sleep_after = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";

	private string GUID_Turnoffdisplayafter = "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e";

	private FileSystemWatcher watcher;

	private FileSystemWatcher watcher_DST_result;

	private string NVME_SN = string.Empty;

	public NotifyIcon nIcon;

	private List<Adapter> adapters;

	public bool showAllAdapter;

	private ManagementObjectCollection queryPowerManagementCollection;

	private ManagementObjectCollection queryWakeCollection;

	private static string g_strTuneUpfolder = string.Empty;

	private ReadWriteIni writeIni = new ReadWriteIni();

	private string PLUGINSUPPORT = "Plugin Support";

	private string RecordSection = "PC_Checkup";

	private string RecordResultPrefix = "Result_";

	private string RecordTimePrefix = "Time_";

	private static string LogPrefix = "DiagnosticManager, ";

	public static bool HDDTesting = false;

	public string HDD_TYPE = "HardDisk";

	public string NETWORK_TYPE = "Network";

	public string LAN_TYPE = "Ethernet Interface Card";

	public string WIFI_TYPE = "WiFi Interface Card";

	public string MOBILE_BROADBAND_TYPE = "Mobile Broadband Device";

	public string BATTERY_TYPE = "BatteryManager";

	public string JunkFileSizeSection = "JunkFileSize";

	public ErrorTypeHandler ErrorEvent;

	public TestProgressEventHandler TestProgressEvent;

	public DeviceInfoUpdateEventArgsHandler DeviceInfoUpdateEvent;

	public bool SingleInstance()
	{
		singleInstance = new SingleInstance("{9D8FBB56-3126-46E0-8248-6DE7CBBE8A60}");
		return singleInstance.IsSingleInstance;
	}

	[DllImport("kernel32", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetPrivateProfileStringW", ExactSpelling = true, SetLastError = true)]
	private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, string lpReturnString, int nSize, string lpFilename);

	[DllImport("kernel32", CharSet = CharSet.Unicode)]
	private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);

	private Stream GetIconStream()
	{
		if (_model == "ACC")
		{
			_brand = "Acer";
		}
		string uriString = $"pack://application:,,,/ACCStd;component/Resource/Images/{_model}/{_brand}/icon.ico";
		return Application.GetResourceStream(new Uri(uriString)).Stream;
	}

	private Stream GetIconTrayStream()
	{
		if (_model == "ACC")
		{
			_brand = "Acer";
		}
		string uriString = $"pack://application:,,,/ACCStd;component/Resource/Images/{_model}/{_brand}/icon_tray.ico";
		return Application.GetResourceStream(new Uri(uriString)).Stream;
	}

	private void LoadAppIcon()
	{
		_brand = CheckEnvironment.Instance.CheckBrand();
		_model = "ACtC";
		if (CheckEnvironment.Instance.CheckModelTypeStr().ToLowerInvariant().Contains("consumer"))
		{
			_model = "ACC";
		}
		if (_brand.ToLowerInvariant().Contains("acer"))
		{
			_appName = "Acer";
		}
		else if (_brand.ToLowerInvariant().Contains("gateway"))
		{
			_appName = "Gateway";
		}
		else if (_brand.ToLowerInvariant().Contains("packard bell"))
		{
			_appName = "Packard Bell";
		}
		else if (_brand.ToLowerInvariant().Contains("founder"))
		{
			_appName = "Founder";
		}
		else
		{
			_appName = "Acer";
		}
		if (_model == "ACtC")
		{
			_appName += " ControlCenter";
		}
		else
		{
			_appName += " Care Center";
		}
	}

	private void UpdateDriveInfoToIni(List<HDDDeviceInfo> HDDList, bool isDetail)
	{
		if (HDDList.Count == 0)
		{
			Log.i("[ACCStd][UpdateDriveInfoToIni] DriveList is empty");
			return;
		}
		string text = (isDetail ? "DriveInfo_Detail_ADS" : "DriveInfo_ADS");
		for (int i = 0; i < 20; i++)
		{
			writeIni.WriteIni_WMILog_ADS(text, "HDD" + (i + 1), (string)null);
			writeIni.WriteIni_WMILog_ADS(text, "HDDpartition" + (i + 1), (string)null);
			writeIni.WriteIni_WMILog_ADS(text, "HDDdeviceType" + (i + 1), (string)null);
			writeIni.WriteIni_WMILog_ADS(text, "HDDSN" + (i + 1), (string)null);
		}
		for (int j = 0; j < HDDList.Count; j++)
		{
			writeIni.WriteIni_WMILog_ADS(text, "HDD" + (j + 1), HDDList[j]._HDDName);
			writeIni.WriteIni_WMILog_ADS(text, "HDDpartition" + (j + 1), HDDList[j]._Partition);
			writeIni.WriteIni_WMILog_ADS(text, "HDDdeviceType" + (j + 1), HDDList[j]._DeviceType.ToString("F"));
			writeIni.WriteIni_WMILog_ADS(text, "HDDSN" + (j + 1), HDDList[j]._HDDSN);
		}
	}

	private void StartMonitorDriveInfo_ADS()
	{
		Log.d("StartMonitorDriveInfo_ADS()");
		if (!Monitor_ADS)
		{
			Log.d("Monitor_ADS:" + Monitor_ADS);
			Monitor_ADS = true;
			TimerCallback callback = Update;
			_timeForDriveChange = new System.Threading.Timer(callback);
			_timeForDriveChange.Change(0, 20000);
		}
	}

	private void Update(object state)
	{
		List<HDDDeviceInfo> hDDInformation = DiagnosticManager_ACCStd.Instance.GetHDDInformation();
		List<HDDDeviceInfo> hDDInformation_Detail_Version = DiagnosticManager_ACCStd.Instance.GetHDDInformation_Detail_Version();
		bool flag = false;
		if (!getHDDInformation && hDDInformation_Detail_Version.Count != 0)
		{
			Log.d("Temp_detail.Count != 0");
			for (int i = 0; i < hDDInformation_Detail_Version.Count; i++)
			{
				Log.d("HDD" + (i + 1) + " " + hDDInformation_Detail_Version[i]._HDDName);
				Log.d("HDDdeviceType" + (i + 1) + " " + hDDInformation_Detail_Version[i]._DeviceType.ToString("F"));
				if (hDDInformation_Detail_Version[i]._DeviceType.ToString("F") == "UnKnown")
				{
					flag = true;
					getHDDInformation = true;
					break;
				}
			}
			if (flag)
			{
				Log.d("HDD's type is UNKNOWN and refresh to getDiskInfomation");
				DiagnosticManager_ACCStd.Instance.GetDiskInfomation();
			}
			else
			{
				Log.d("HDD's type not UNKNOWN");
				_delayTriggerChange = true;
			}
		}
		if (!_delayTriggerChange && hDDInformation.Count != 0)
		{
			_delayTriggerChange = true;
		}
		if (_currentDriveList == null)
		{
			_currentDriveList = hDDInformation;
		}
		else
		{
			try
			{
				_currentDriveList = null;
				_currentDriveList = hDDInformation;
				Log.d("HDD Temp.Count:" + hDDInformation.Count);
				Log.d("HDD _currentDriveList.Count:" + _currentDriveList.Count);
			}
			catch (Exception ex)
			{
				Log.d("Error:" + ex.Message);
			}
		}
		if (!_delayTriggerChange && hDDInformation_Detail_Version.Count != 0)
		{
			_delayTriggerChange = true;
		}
		if (_currentDriveList_detail == null)
		{
			_currentDriveList_detail = hDDInformation_Detail_Version;
		}
		else
		{
			try
			{
				_currentDriveList_detail = null;
				_currentDriveList_detail = hDDInformation_Detail_Version;
				Log.d("_currentDriveList.Count:" + _currentDriveList_detail.Count);
				Log.d("_currentDriveList_detail = Temp_detail");
			}
			catch (Exception ex2)
			{
				Log.d("Error:" + ex2.Message);
			}
		}
		UpdateDriveInfoToIni(_currentDriveList, isDetail: false);
		UpdateDriveInfoToIni(_currentDriveList_detail, isDetail: true);
		if (_currentDriveList.Count != 0 && _currentDriveList_detail.Count != 0 && !flag)
		{
			Monitor_ADS = false;
			StopMonitorDriveDriveInfo_ADS();
			Log.i("[ACCStd][Update] DriveList initial finish");
		}
	}

	private void StopMonitorDriveDriveInfo_ADS()
	{
		if (_timeForDriveChange != null)
		{
			_timeForDriveChange.Dispose();
		}
	}

	private void StartMonitorSelfUpgrade()
	{
		TimerCallback callback = PeriodCheckSelfupgrade;
		_timeForSelfupgradeCheck = new System.Threading.Timer(callback);
		_timeForSelfupgradeCheck.Change(0, 10080 * _delayTime);
	}

	private void StopMonitorSelfUpgrade()
	{
		if (_timeForSelfupgradeCheck != null)
		{
			_timeForSelfupgradeCheck.Dispose();
		}
	}

	public void WMIManagementWMI()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected Obj, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected Obj, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected Obj, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected Obj, but got Unknown
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected Obj, but got Unknown
		ConnectionOptions val = new ConnectionOptions();
		val.Impersonation = (ImpersonationLevel)3;
		val.EnablePrivileges = true;
		bar = new ManagementScope("\\root\\wmi", val);
		bar.Connect();
		queryPowerManagement = new ManagementObjectSearcher(bar, (ObjectQuery)new WqlObjectQuery("SELECT * FROM MSPower_DeviceEnable"));
		queryWake = new ManagementObjectSearcher(bar, (ObjectQuery)new WqlObjectQuery("SELECT * FROM MSPower_DeviceWakeEnable"));
	}

	public MainWindow()
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected Obj, but got Unknown
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected Obj, but got Unknown
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Expected Obj, but got Unknown
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Expected Obj, but got Unknown
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Expected Obj, but got Unknown
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Expected Obj, but got Unknown
		Log.i("[ACCStd] MainWindow start");
		if (!SingleInstance())
		{
			Log.i("[ACCStd] !SingleInstance()");
			((Window)this).Close();
			return;
		}
		if (UserCompetence.IsAuditMode())
		{
			((Window)this).Close();
			return;
		}
		Directory.SetCurrentDirectory(CheckEnvironment.Instance.GetInstallPath());
		string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		AppDomain currentDomain = AppDomain.CurrentDomain;
		currentDomain.AppendPrivatePath(directoryName + "\\ACCPlugin");
		Read_iniCalibrationKey();
		Check_Storage_list();
		check_DST_ini();
		Support_NVME_DST();
		LoadAppIcon();
		wmimanagementwmi = new WMIManagementWMI();
		checkBIOSWakeOnLan();
		checkBIOS();
		updateAdapters();
		IsSupportGetHardDriveInfo();
		Launch_FixpackFolderCleaner();
		if (UserCompetence.IsNeedToDoSelfUpgrade())
		{
			StartMonitorSelfUpgrade();
		}
		InitializeComponent();
		initial_icon(GetIconStream());
		StartMonitorDriveInfo_ADS();
		CalJunkFileSize();
		GetXMLStartupInfo();
		ModelType val = CheckEnvironment.Instance.CheckModelType();
		if (!CheckEnvironment.Instance.CheckBrand().ToLowerInvariant().Contains("founder") && (int)val != 0)
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(CommDef._RegPath_Agent);
			Log.i("[ACCStd] reg" + CommDef._RegPath_Agent);
			if (registryKey == null)
			{
				registryKey = Registry.LocalMachine.CreateSubKey(CommDef._RegPath_Agent);
			}
			else
			{
				Log.i("[ACCStd] RM_Pairing.Start()");
				RegistryMonitor val2 = new RegistryMonitor(registryKey);
				val2.RegChanged += RM_AOMIsConnectRegChanged;
				val2.Start();
				GetAuthKey_ACCStd();
			}
		}
		if (!File.Exists("C:\\ProgramData\\Acer\\test.tag"))
		{
			DiagnosticManager_ACCStd.Instance.GetSpecificDeviceInfo(DiagnosticManager_ACCStd.DeviceType.HardDisk);
			DiagnosticManager_ACCStd_for_Memory.Instance.GetSpecificDeviceInfo(DiagnosticManager_ACCStd_for_Memory.DeviceType.RAM);
		}
		_mainProgramNameACC = directoryName + "\\CareCenter.exe";
		_mainProgramNameACtC = directoryName + "\\ControlCenter.exe";
		_pipeServer = new PipeServer();
		_pipeServer.Listen(PipeDef._ACCPipeName);
		_pipeServer.PipeMessage_UI_To_BG += _pipeServer_PipeMessage;
		System.Timers.Timer timer = new System.Timers.Timer();
		timer.Interval = 3000.0;
		timer.Elapsed += delayStart_Elapsed;
		timer.Start();
		RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\OEM\\CareCenter", writable: true);
		if (registryKey2 == null)
		{
			registryKey2 = Registry.CurrentUser.CreateSubKey("Software\\OEM\\CareCenter");
		}
		string value = Assembly.GetExecutingAssembly().GetName().Version.ToString();
		registryKey2.SetValue("Application Version", value, RegistryValueKind.String);
		RegistryKey registryKey3 = Registry.CurrentUser.OpenSubKey("Software\\OEM\\CareCenter\\Notification");
		if (registryKey3 == null)
		{
			registryKey3 = Registry.CurrentUser.CreateSubKey("Software\\OEM\\CareCenter\\Notification");
		}
		RegistryMonitor val3 = new RegistryMonitor(registryKey3);
		val3.RegChanged += RM_RegChanged;
		val3.Start();
		string text = Path.Combine(PACKAGES, "AcerIncorporated.AcerExplorer_48frkmn4z8aw4");
		Log.i("[ACCStd] AEPath: " + text);
		if (Directory.Exists(text))
		{
			try
			{
				fsw.Path = text + "\\LocalState";
				fsw.Filter = "ACC.xml";
				fsw.EnableRaisingEvents = true;
				fsw.Created += fsw_Created;
				Log.i("[ACCStd] fsw.Path = " + fsw.Path.ToString());
			}
			catch (Exception ex)
			{
				Log.e("[ACCStd] MainWindow FileSystemWatcher exception: " + ex.Message);
			}
			Log.i("[ACCStd] AEPath exist !!!");
		}
		Log.i("[ACCStd] MainWindow End");
		if (Read_regeditHWDetectionKey() && CheckEnvironment.Instance.CheckModelTypeStr() == "Commercial")
		{
			Log.i("[ACCStd] Commercial and  Read_regeditHWDetectionKey");
			InitialHardWareInfor();
		}
		Thread.Sleep(3000);
		if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)19) || ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)15) || ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)14) || ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", (ushort)13))
		{
			Log.i("[ACCStd] Running on Win11\n ");
			if (UserCompetence.IsLauncedACCSTD())
			{
				if (UserCompetence.IsFirstTimeLaunchSWA() && _model == "ACtC")
				{
					Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\OEM\\CareCenter", "FirstLaunchSWA", 1, RegistryValueKind.DWord);
					Launch_SoftwareAssembler_ACCUI();
				}
			}
			else
			{
				Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\OEM\\CareCenter", "ACCSTD_Launched", 1, RegistryValueKind.DWord);
			}
		}
		else
		{
			Log.i("[ACCStd] Running on Win11 before\n ");
			if (UserCompetence.IsFirstTimeLaunchSWA() && _model == "ACtC")
			{
				Registry.SetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\OEM\\CareCenter", "FirstLaunchSWA", 1, RegistryValueKind.DWord);
				Launch_SoftwareAssembler_ACCUI();
			}
		}
	}

	private bool Read_regeditHWDetectionKey()
	{
		RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\\\OEM\\\\CareCenter\\\\HWChangedetection");
		if (registryKey != null)
		{
			string text = registryKey.GetValue("HWChangedetection_status") as string;
			if (text == "ON")
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public static void GetDeviceHardDrive(ref List<HardDriveName> _dicHardDriveInfo)
	{
		List<HardDriveName> list = new List<HardDriveName>();
		try
		{
			List<string> strResult = new List<string>();
			List<string> strResult2 = new List<string>();
			List<string> strResult3 = new List<string>();
			List<string> strResult4 = new List<string>();
			List<string> strResult5 = new List<string>();
			lock (_pHardwareDeviceLock)
			{
				try
				{
					Get_WMI_Info("select * from Win32_DiskDrive where (MediaType != NULL)", "InterfaceType", ref strResult2);
				}
				catch
				{
					Log.e("[ACCUtilities] Hard drive InterfaceType Get Error");
				}
				try
				{
					Get_WMI_Info("select * from Win32_DiskDrive where (MediaType != NULL)", "Model", ref strResult);
				}
				catch
				{
					Log.e("[ACCUtilities] Hard drive Model Get Error");
				}
				try
				{
					Get_WMI_Info("select * from Win32_DiskDrive where (MediaType != NULL)", "Index", ref strResult3);
				}
				catch
				{
					Log.e("[ACCUtilities] Hard drive Index Get Error");
				}
				try
				{
					Get_WMI_Info("select * from Win32_DiskDrive where (MediaType != NULL)", "MediaType", ref strResult4);
				}
				catch
				{
					Log.e("[ACCUtilities] Hard drive Index Get Error");
				}
				try
				{
					Get_WMI_Info("select SerialNumber from Win32_DiskDrive where (MediaType != NULL)", "SerialNumber", ref strResult5);
				}
				catch
				{
					Log.e("[ACCUtilities] Hard drive SerialNumber Get Error");
				}
			}
			int num = 0;
			while (num < strResult.Count)
			{
				for (int i = 0; i < strResult.Count; i++)
				{
					if (Convert.ToInt32(strResult3[i]) == num)
					{
						num++;
						list.Add(new HardDriveName(strResult[i], strResult2[i], strResult4[i], strResult3[i], strResult5[i]));
						Log.i("[ACCStd] HardDrive:" + strResult[i] + " HardDriveType:" + strResult2[i]);
						break;
					}
					if (i == strResult.Count - 1)
					{
						num++;
					}
				}
			}
			_dicHardDriveInfo = list;
		}
		catch
		{
		}
	}

	private static void Get_WMI_Info(string strQueryLocation, string strQueryNode, ref List<string> strResult)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected Obj, but got Unknown
		ManagementObjectEnumerator enumerator = new ManagementObjectSearcher(strQueryLocation).Get().GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				ManagementObject val = (ManagementObject)enumerator.Current;
				try
				{
					strResult.Add(((ManagementBaseObject)val)[strQueryNode].ToString());
				}
				catch
				{
					strResult.Add(" ");
				}
			}
		}
		finally
		{
			((IDisposable)enumerator)?.Dispose();
		}
	}

	private void InitialHardWareInfor()
	{
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0816: Unknown result type (might be due to invalid IL or missing references)
		//IL_081d: Expected Obj, but got Unknown
		//IL_081d: Unknown result type (might be due to invalid IL or missing references)
		//IL_082d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0832: Unknown result type (might be due to invalid IL or missing references)
		//IL_083e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0843: Unknown result type (might be due to invalid IL or missing references)
		//IL_084f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0854: Unknown result type (might be due to invalid IL or missing references)
		//IL_0886: Unknown result type (might be due to invalid IL or missing references)
		//IL_088d: Expected Obj, but got Unknown
		//IL_089f: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ab: Expected Obj, but got Unknown
		//IL_08d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_08dc: Expected Obj, but got Unknown
		//IL_08ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fa: Expected Obj, but got Unknown
		//IL_0914: Unknown result type (might be due to invalid IL or missing references)
		//IL_091b: Expected Obj, but got Unknown
		//IL_092d: Unknown result type (might be due to invalid IL or missing references)
		//IL_092f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0939: Expected Obj, but got Unknown
		//IL_09a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ac: Expected Obj, but got Unknown
		//IL_09be: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ca: Expected Obj, but got Unknown
		//IL_09f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fb: Expected Obj, but got Unknown
		//IL_0a0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a19: Expected Obj, but got Unknown
		//IL_0a33: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a3a: Expected Obj, but got Unknown
		//IL_0a4c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a4e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a58: Expected Obj, but got Unknown
		//IL_0ac4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0acb: Expected Obj, but got Unknown
		//IL_0add: Unknown result type (might be due to invalid IL or missing references)
		//IL_0adf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae9: Expected Obj, but got Unknown
		//IL_0c63: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6a: Expected Obj, but got Unknown
		//IL_0c7c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c7e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c88: Expected Obj, but got Unknown
		//IL_0fb1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fb8: Expected Obj, but got Unknown
		//IL_0e04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e0b: Expected Obj, but got Unknown
		//IL_0e1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e29: Expected Obj, but got Unknown
		//IL_0b93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9a: Expected Obj, but got Unknown
		//IL_0bac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb8: Expected Obj, but got Unknown
		//IL_0bd2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd9: Expected Obj, but got Unknown
		//IL_0beb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf7: Expected Obj, but got Unknown
		//IL_0d34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d3b: Expected Obj, but got Unknown
		//IL_0d4d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d59: Expected Obj, but got Unknown
		//IL_0d73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7a: Expected Obj, but got Unknown
		//IL_0d8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d98: Expected Obj, but got Unknown
		//IL_0ed5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edc: Expected Obj, but got Unknown
		//IL_0eee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ef0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efa: Expected Obj, but got Unknown
		//IL_0f14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f1b: Expected Obj, but got Unknown
		//IL_0f2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f2f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f39: Expected Obj, but got Unknown
		Log.i("[ACCStd] InitialHardWareInfor ");
		string path = "C:\\ProgramData\\Acer\\CareCenter\\HardWareInfo.ini";
		if (File.Exists(path))
		{
			List<HardDriveName> _dicHardDriveInfo = new List<HardDriveName>();
			GetDeviceHardDrive(ref _dicHardDriveInfo);
			List<MySystemInfoStructure> list = new List<MySystemInfoStructure>();
			list = HardwareDeviceManager.GetSystemHardwareInfoList();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			string text = "";
			string text2 = "";
			List<string> list5 = new List<string>();
			List<string> list6 = new List<string>();
			List<string> list7 = new List<string>();
			string text3 = "";
			string text4 = "";
			string text5 = "";
			string text6 = "";
			string text7 = "";
			string text8 = "";
			string text9 = "";
			int num = 1;
			while (true)
			{
				writeIni.GetHardwareInfofromDrives(ref text5, num);
				num++;
				if (num > 20 || text5 == "NULL")
				{
					break;
				}
				list2.Add(text5);
			}
			num = 1;
			while (true)
			{
				writeIni.GetHardwareInfofromGraphics(ref text8, num);
				num++;
				if (num > 5 || text8 == "NULL")
				{
					break;
				}
				list3.Add(text8);
			}
			num = 1;
			while (true)
			{
				writeIni.GetHardwareInfofromNetwork(ref text9, num);
				num++;
				if (num > 20 || text9 == "NULL")
				{
					break;
				}
				list4.Add(text9);
			}
			if (writeIni.GetHardwareInfofromProcessor(ref text6))
			{
				text = text6;
			}
			if (writeIni.GetHardwareInfofromMemory(ref text7))
			{
				text2 = text7;
			}
			writeIni.ResetHardwareData();
			int num2 = 1;
			for (int i = 0; i < _dicHardDriveInfo.Count; i++)
			{
				Log.i("[ACCStd] HardDriveName: " + _dicHardDriveInfo[i].HardModelName + " HardDriveType: " + _dicHardDriveInfo[i].HardDriveType + " MediaType: " + _dicHardDriveInfo[i].MediaType);
				if (_dicHardDriveInfo[i].HardDriveType != "USB" && _dicHardDriveInfo[i].MediaType != "Removable Media")
				{
					Log.i("[ACCStd] HardDrive not USB:" + _dicHardDriveInfo[i].HardModelName);
					writeIni.WriteHardWareInfoIni("Drives", "Hard" + num2, _dicHardDriveInfo[i].HardModelName);
					if (_dicHardDriveInfo[i].HardModelName == "")
					{
						list5.Add("Unknown device");
					}
					else
					{
						list5.Add(_dicHardDriveInfo[i].HardModelName);
					}
					num2++;
				}
				else
				{
					Log.i("[ACCStd] HardDrive is USB:" + _dicHardDriveInfo[i].HardModelName);
				}
			}
			int num3 = 1;
			int num4 = 1;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].m_strDeviceName == "Processor")
				{
					writeIni.WriteHardWareInfoIni("Processor", list[j].m_strDeviceName, list[j].m_strDeviceValue);
					text3 = list[j].m_strDeviceValue;
				}
				else if (list[j].m_strDeviceName == "GraphicsDevice")
				{
					writeIni.WriteHardWareInfoIni("GraphicsDevice", list[j].m_strDeviceName + num3, list[j].m_strDeviceValue);
					if (list[j].m_strDeviceValue == "")
					{
						list6.Add("Unknown device");
					}
					else
					{
						list6.Add(list[j].m_strDeviceValue);
					}
					num3++;
				}
				else if (list[j].m_strDeviceName == "NetworkDevice")
				{
					writeIni.WriteHardWareInfoIni("NetworkDevice", list[j].m_strDeviceName + num4, list[j].m_strDeviceValue);
					if (list[j].m_strDeviceValue == "")
					{
						list7.Add("Unknown device");
					}
					else
					{
						list7.Add(list[j].m_strDeviceValue);
					}
					num4++;
					Log.i("[XXXXXXXX][ACCStd]InitialHardWareInfor(): KnowMySystemInfo[i].m_strDeviceValue: " + list[j].m_strDeviceValue);
				}
				else if (list[j].m_strDeviceName == "Memory")
				{
					writeIni.WriteHardWareInfoIni("Memory", list[j].m_strDeviceName, list[j].m_strDeviceValue);
					text4 = list[j].m_strDeviceValue;
				}
			}
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = false;
			Log.i("[ACCStd] ini_Processor" + text + " " + text.Length);
			Log.i("[ACCStd] sys_Processor" + text3 + " " + text3.Length);
			if (text != text3.Trim())
			{
				flag = true;
			}
			if (text2 != text4.Trim())
			{
				flag2 = true;
			}
			for (int k = 0; k < list2.Count; k++)
			{
				bool flag6 = false;
				for (int l = 0; l < list5.Count; l++)
				{
					if (list2[k] == list5[l].Trim())
					{
						flag6 = true;
						break;
					}
				}
				if (!flag6)
				{
					flag5 = true;
				}
			}
			for (int m = 0; m < list3.Count; m++)
			{
				bool flag7 = false;
				for (int n = 0; n < list6.Count; n++)
				{
					if (list3[m] == list6[n].Trim())
					{
						flag7 = true;
						break;
					}
				}
				if (!flag7)
				{
					flag3 = true;
				}
			}
			for (int num5 = 0; num5 < list4.Count; num5++)
			{
				bool flag8 = false;
				for (int num6 = 0; num6 < list7.Count; num6++)
				{
					if (list4[num5] == list7[num6].Trim())
					{
						flag8 = true;
						break;
					}
				}
				if (!flag8)
				{
					flag4 = true;
				}
			}
			for (int num7 = 0; num7 < list5.Count; num7++)
			{
				bool flag9 = false;
				for (int num8 = 0; num8 < list2.Count; num8++)
				{
					if (list5[num7] == list2[num8].Trim())
					{
						flag9 = true;
						break;
					}
				}
				if (!flag9)
				{
					flag5 = true;
				}
			}
			for (int num9 = 0; num9 < list6.Count; num9++)
			{
				bool flag10 = false;
				for (int num10 = 0; num10 < list3.Count; num10++)
				{
					if (list6[num9] == list3[num10].Trim())
					{
						flag10 = true;
						break;
					}
				}
				if (!flag10)
				{
					flag3 = true;
				}
			}
			for (int num11 = 0; num11 < list7.Count; num11++)
			{
				Log.i("[XXXXXXXX][ACCStd]InitialHardWareInfor(): sys_NetworkDevice[i]: " + list7[num11]);
				bool flag11 = false;
				for (int num12 = 0; num12 < list4.Count; num12++)
				{
					Log.i("[XXXXXXXX][ACCStd]InitialHardWareInfor(): ini_NetworkDevice[j].Trim(): " + list4[num12].Trim());
					if (list7[num11] == list4[num12].Trim())
					{
						Log.i("[XXXXXXXX][ACCStd]InitialHardWareInfor(): flag = true ");
						flag11 = true;
						break;
					}
				}
				if (!flag11)
				{
					flag4 = true;
				}
				Log.i("[XXXXXXXX][ACCStd]InitialHardWareInfor(): ChangeNetworkDevice: " + flag4);
			}
			if (!flag && !flag2 && !flag3 && !flag4 && !flag5)
			{
				return;
			}
			TextBlock val = new TextBlock();
			new TransferData();
			Color val2 = (Color)ColorConverter.ConvertFromString("#9bd71e");
			Color val3 = (Color)ColorConverter.ConvertFromString("#999999");
			Color val4 = (Color)ColorConverter.ConvertFromString("#FFFFFF");
			if (flag)
			{
				string text10 = (string)Application.Current.FindResource((object)"MySystem_Processor");
				InlineCollection inlines = val.Inlines;
				Run val5 = new Run(text10 + "\n");
				((TextElement)val5).FontSize = 16.0;
				((TextElement)val5).Foreground = (Brush)new SolidColorBrush(val4);
				((TextElementCollection<Inline>)(object)inlines).Add((Inline)(object)val5);
				string text11 = text + " ► " + text3;
				InlineCollection inlines2 = val.Inlines;
				Run val6 = new Run(text + "\n");
				((TextElement)val6).FontSize = 13.0;
				((TextElement)val6).Foreground = (Brush)new SolidColorBrush(val3);
				((TextElementCollection<Inline>)(object)inlines2).Add((Inline)(object)val6);
				InlineCollection inlines3 = val.Inlines;
				Run val7 = new Run("►" + text3);
				((TextElement)val7).FontSize = 13.0;
				((TextElement)val7).Foreground = (Brush)new SolidColorBrush(val2);
				((TextElementCollection<Inline>)(object)inlines3).Add((Inline)(object)val7);
				LogHistory.hardwareIn((string)Application.Current.FindResource((object)"MUI_Asset_HWChanged"), text11, text10, "Hardwarechange", true, (iconImg)0);
				val.Inlines.Add("\n\n");
			}
			if (flag2)
			{
				string text12 = (string)Application.Current.FindResource((object)"MySystem_Memory");
				InlineCollection inlines4 = val.Inlines;
				Run val8 = new Run(text12 + "\n");
				((TextElement)val8).FontSize = 16.0;
				((TextElement)val8).Foreground = (Brush)new SolidColorBrush(val4);
				((TextElementCollection<Inline>)(object)inlines4).Add((Inline)(object)val8);
				string text13 = text2 + " ► " + text4;
				InlineCollection inlines5 = val.Inlines;
				Run val9 = new Run(text2 + "\n");
				((TextElement)val9).FontSize = 13.0;
				((TextElement)val9).Foreground = (Brush)new SolidColorBrush(val3);
				((TextElementCollection<Inline>)(object)inlines5).Add((Inline)(object)val9);
				InlineCollection inlines6 = val.Inlines;
				Run val10 = new Run("►" + text4);
				((TextElement)val10).FontSize = 13.0;
				((TextElement)val10).Foreground = (Brush)new SolidColorBrush(val2);
				((TextElementCollection<Inline>)(object)inlines6).Add((Inline)(object)val10);
				LogHistory.hardwareIn((string)Application.Current.FindResource((object)"MUI_Asset_HWChanged"), text13, text12, "Hardwarechange", true, (iconImg)0);
				val.Inlines.Add("\n\n");
			}
			if (flag5)
			{
				string text14 = (string)Application.Current.FindResource((object)"MySystem_HardDrive");
				InlineCollection inlines7 = val.Inlines;
				Run val11 = new Run(text14 + "\n");
				((TextElement)val11).FontSize = 16.0;
				((TextElement)val11).Foreground = (Brush)new SolidColorBrush(val4);
				((TextElementCollection<Inline>)(object)inlines7).Add((Inline)(object)val11);
				string text15 = "";
				for (int num13 = 0; num13 < list2.Count; num13++)
				{
					if (num13 != 0)
					{
						text15 += ", ";
					}
					text15 += list2[num13];
				}
				string text16 = "";
				for (int num14 = 0; num14 < list5.Count; num14++)
				{
					if (num14 != 0)
					{
						text16 += ", ";
					}
					text16 += list5[num14];
				}
				string text17 = text15 + " ► " + text16;
				InlineCollection inlines8 = val.Inlines;
				Run val12 = new Run(text15 + "\n");
				((TextElement)val12).FontSize = 13.0;
				((TextElement)val12).Foreground = (Brush)new SolidColorBrush(val3);
				((TextElementCollection<Inline>)(object)inlines8).Add((Inline)(object)val12);
				InlineCollection inlines9 = val.Inlines;
				Run val13 = new Run("►" + text16);
				((TextElement)val13).FontSize = 13.0;
				((TextElement)val13).Foreground = (Brush)new SolidColorBrush(val2);
				((TextElementCollection<Inline>)(object)inlines9).Add((Inline)(object)val13);
				LogHistory.hardwareIn((string)Application.Current.FindResource((object)"MUI_Asset_HWChanged"), text17, text14, "Hardwarechange", true, (iconImg)0);
				val.Inlines.Add("\n\n");
			}
			if (flag3)
			{
				string text18 = (string)Application.Current.FindResource((object)"MySystem_Graphics");
				InlineCollection inlines10 = val.Inlines;
				Run val14 = new Run(text18 + "\n");
				((TextElement)val14).FontSize = 16.0;
				((TextElement)val14).Foreground = (Brush)new SolidColorBrush(val4);
				((TextElementCollection<Inline>)(object)inlines10).Add((Inline)(object)val14);
				string text19 = "";
				for (int num15 = 0; num15 < list3.Count; num15++)
				{
					if (num15 != 0)
					{
						text19 += ", ";
					}
					text19 += list3[num15];
				}
				string text20 = "";
				for (int num16 = 0; num16 < list6.Count; num16++)
				{
					if (num16 != 0)
					{
						text20 += ", ";
					}
					text20 += list6[num16];
				}
				string text21 = text19 + " ► " + text20;
				InlineCollection inlines11 = val.Inlines;
				Run val15 = new Run(text19 + "\n");
				((TextElement)val15).FontSize = 13.0;
				((TextElement)val15).Foreground = (Brush)new SolidColorBrush(val3);
				((TextElementCollection<Inline>)(object)inlines11).Add((Inline)(object)val15);
				InlineCollection inlines12 = val.Inlines;
				Run val16 = new Run("►" + text20);
				((TextElement)val16).FontSize = 13.0;
				((TextElement)val16).Foreground = (Brush)new SolidColorBrush(val2);
				((TextElementCollection<Inline>)(object)inlines12).Add((Inline)(object)val16);
				LogHistory.hardwareIn((string)Application.Current.FindResource((object)"MUI_Asset_HWChanged"), text21, text18, "Hardwarechange", true, (iconImg)0);
				val.Inlines.Add("\n\n");
			}
			if (flag4)
			{
				string text22 = (string)Application.Current.FindResource((object)"MySystem_NetworkDevice");
				InlineCollection inlines13 = val.Inlines;
				Run val17 = new Run(text22 + "\n");
				((TextElement)val17).FontSize = 16.0;
				((TextElement)val17).Foreground = (Brush)new SolidColorBrush(val4);
				((TextElementCollection<Inline>)(object)inlines13).Add((Inline)(object)val17);
				string text23 = "";
				for (int num17 = 0; num17 < list4.Count; num17++)
				{
					if (num17 != 0)
					{
						text23 += ", ";
					}
					text23 += list4[num17];
				}
				string text24 = "";
				for (int num18 = 0; num18 < list7.Count; num18++)
				{
					if (num18 != 0)
					{
						text24 += ", ";
					}
					text24 += list7[num18];
				}
				string text25 = text23 + " ► " + text24;
				InlineCollection inlines14 = val.Inlines;
				Run val18 = new Run(text23 + "\n");
				((TextElement)val18).FontSize = 13.0;
				((TextElement)val18).Foreground = (Brush)new SolidColorBrush(val3);
				((TextElementCollection<Inline>)(object)inlines14).Add((Inline)(object)val18);
				InlineCollection inlines15 = val.Inlines;
				Run val19 = new Run("►" + text24);
				((TextElement)val19).FontSize = 13.0;
				((TextElement)val19).Foreground = (Brush)new SolidColorBrush(val2);
				((TextElementCollection<Inline>)(object)inlines15).Add((Inline)(object)val19);
				LogHistory.hardwareIn((string)Application.Current.FindResource((object)"MUI_Asset_HWChanged"), text25, text22, "Hardwarechange", true, (iconImg)0);
			}
			Log.i("[ACCStd] DialogType_E2");
			Dialog.Box.New_DialogTyp_HWChangeDetection();
			HowItWorkWindowUserControl howItWorkWindowUserControl = new HowItWorkWindowUserControl(val, string.Empty);
			arg val20 = new arg((string)Application.Current.FindResource((object)"MUI_Asset_HWChanged"), (UserControl)(object)howItWorkWindowUserControl, (string)null, (string)Application.Current.FindResource((object)"MUI_Asset_Confirm"));
			Dialog.Box._Show((Type)9, (object)val20);
		}
		else
		{
			GetHardwareInformation();
		}
	}

	private void GetHardwareInformation()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		Log.i("[ACCStd] GetHardwareInformation ");
		List<HardDriveName> _dicHardDriveInfo = new List<HardDriveName>();
		GetDeviceHardDrive(ref _dicHardDriveInfo);
		List<MySystemInfoStructure> list = new List<MySystemInfoStructure>();
		list = HardwareDeviceManager.GetSystemHardwareInfoList();
		int num = 1;
		for (int i = 0; i < _dicHardDriveInfo.Count; i++)
		{
			if (_dicHardDriveInfo[i].HardDriveType != "USB" && _dicHardDriveInfo[i].MediaType != "Removable Media")
			{
				writeIni.WriteHardWareInfoIni("Drives", "Hard" + num, _dicHardDriveInfo[i].HardModelName);
				num++;
			}
		}
		num = 1;
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].m_strDeviceName == "GraphicsDevice")
			{
				writeIni.WriteHardWareInfoIni("GraphicsDevice", list[j].m_strDeviceName + num, list[j].m_strDeviceValue);
				num++;
			}
		}
		num = 1;
		for (int k = 0; k < list.Count; k++)
		{
			if (list[k].m_strDeviceName == "NetworkDevice")
			{
				writeIni.WriteHardWareInfoIni("NetworkDevice", list[k].m_strDeviceName + num, list[k].m_strDeviceValue);
				num++;
			}
		}
		for (int l = 0; l < list.Count; l++)
		{
			if (list[l].m_strDeviceName == "Processor")
			{
				writeIni.WriteHardWareInfoIni("Processor", list[l].m_strDeviceName, list[l].m_strDeviceValue);
			}
			Log.i("KnowMySystemInfo[i].m_strDeviceName " + list[l].m_strDeviceValue + " " + list[l].m_strDeviceValue.Length);
			if (list[l].m_strDeviceName == "Memory")
			{
				writeIni.WriteHardWareInfoIni("Memory", list[l].m_strDeviceName, list[l].m_strDeviceValue);
			}
		}
	}

	private void fsw_Created(object sender, FileSystemEventArgs e)
	{
		Log.i("[ACCStd] fsw_Changed START");
		string empty = string.Empty;
		string text = string.Empty;
		string text2 = string.Empty;
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(e.FullPath);
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("CommandArgs/Plugin");
			foreach (XmlNode item in xmlNodeList)
			{
				text = item.InnerText;
			}
			XmlNodeList xmlNodeList2 = xmlDocument.SelectNodes("CommandArgs/Argument");
			foreach (XmlNode item2 in xmlNodeList2)
			{
				text2 = item2.InnerText;
			}
			empty = text + " " + text2;
			if (string.IsNullOrEmpty(empty))
			{
				string mainProgramNameACC = _mainProgramNameACC;
				mainProgramNameACC = ((!File.Exists(_mainProgramNameACC)) ? _mainProgramNameACtC : _mainProgramNameACC);
				try
				{
					ProcessStartInfo startInfo = new ProcessStartInfo(mainProgramNameACC);
					Process.Start(startInfo);
				}
				catch (Exception ex)
				{
					Log.e("[ACCStd] fsw_Changed PSI exception: " + ex.Message);
				}
			}
			else
			{
				string mainProgramNameACC2 = _mainProgramNameACC;
				mainProgramNameACC2 = ((!File.Exists(_mainProgramNameACC)) ? _mainProgramNameACtC : _mainProgramNameACC);
				try
				{
					ProcessStartInfo processStartInfo = new ProcessStartInfo(mainProgramNameACC2);
					processStartInfo.Arguments = empty;
					Process.Start(processStartInfo);
				}
				catch (Exception ex2)
				{
					Log.e("[ACCStd] fsw_Changed PSI (else) exception: " + ex2.Message);
				}
			}
		}
		catch (Exception ex3)
		{
			Log.e("[ACCStd] fsw_Changed exception: " + ex3.Message);
		}
		try
		{
			if (File.Exists(e.FullPath))
			{
				File.Delete(e.FullPath);
			}
		}
		catch (Exception ex4)
		{
			Log.e("[ACCStd] fsw_Changed file delete exception: " + ex4.Message);
		}
		Log.i("[ACCStd] fsw_Changed END");
	}

	private void RM_RegChanged(object sender, EventArgs e)
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)(() =>
		{
			IconChange(GetIconTrayStream());
		}), new object[0]);
	}

	private void RM_AOMIsConnectRegChanged(object sender, EventArgs e)
	{
		Log.i("[ACCStd] RM_AOMIsConnectRegChanged START");
		GetAuthKey_ACCStd();
		Log.i("[ACCStd] RM_AOMIsConnectRegChanged END");
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
	}

	private void _pipeServer_PipeMessage(TransferData args)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected I4, but got Unknown
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0676: Unknown result type (might be due to invalid IL or missing references)
		//IL_067d: Expected Obj, but got Unknown
		//IL_0ad4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0adb: Expected Obj, but got Unknown
		//IL_0adb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae2: Expected Obj, but got Unknown
		//IL_0c95: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c9b: Invalid comparison between Unknown and I4
		//IL_0db6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dbd: Expected Obj, but got Unknown
		//IL_0dfb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e01: Invalid comparison between Unknown and I4
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Expected Obj, but got Unknown
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Expected Obj, but got Unknown
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Expected Obj, but got Unknown
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Expected Obj, but got Unknown
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Expected Obj, but got Unknown
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Expected Obj, but got Unknown
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Expected Obj, but got Unknown
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Expected Obj, but got Unknown
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0464: Expected Obj, but got Unknown
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Expected Obj, but got Unknown
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Expected Obj, but got Unknown
		//IL_0495: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Expected Obj, but got Unknown
		Log.i("[ACCStd] [_pipeServer_PipeMessage] args ==> " + ((object)args.Command).ToString());
		if ((int)args.Command == 0)
		{
			return;
		}
		Commands command = args.Command;
		switch (command - 1)
		{
		case 4:
			IconChange(GetIconStream());
			break;
		case 1:
			MonitorManager.Instance.UpdateMonitorStatus(MonitorEnums.ConvertStringToEnum_Plugins((object)args.PlugName), args.MonitorName, args.argList);
			break;
		case 2:
			MonitorManager.Instance.SetMonitorObjStatue(MonitorEnums.ConvertStringToEnum_Plugins((object)args.PlugName), args.MonitorName, (MonitorStatus)2);
			break;
		case 3:
			MonitorManager.Instance.SetMonitorObjStatue(MonitorEnums.ConvertStringToEnum_Plugins((object)args.PlugName), args.MonitorName, (MonitorStatus)1);
			break;
		case 34:
			GetWMIBatteryHealthControlStatus();
			break;
		case 47:
			Log.i("[ACCStd] GetWMIBatteryCalibrationStatus");
			GetWMIBatteryCalibrationStatus();
			break;
		case 38:
		{
			int healthMode4 = 1;
			bool state_HealtMode4 = true;
			SetWMIBatteryHealthControl(healthMode4, state_HealtMode4);
			TransferData val7 = new TransferData();
			val7.Command = (Commands)37;
			GetWMIBatteryHealthmodeControlStatus(val7);
			break;
		}
		case 39:
		{
			int healthMode3 = 1;
			bool state_HealtMode3 = false;
			SetWMIBatteryHealthControl(healthMode3, state_HealtMode3);
			TransferData val4 = new TransferData();
			val4.Command = (Commands)38;
			GetWMIBatteryHealthmodeControlStatus(val4);
			break;
		}
		case 41:
		{
			int healthMode = 2;
			bool state_HealtMode = true;
			SetWMIBatteryHealthControl(healthMode, state_HealtMode);
			TransferData val = new TransferData();
			val.Command = (Commands)43;
			GetWMIBatteryHealthmodeControlStatus(val);
			break;
		}
		case 40:
		{
			Log.i("[ACCStd] SetWMIBatteryHealthControl");
			int healthMode2 = 2;
			bool state_HealtMode2 = false;
			SetWMIBatteryHealthControl(healthMode2, state_HealtMode2);
			TransferData val3 = new TransferData();
			val3.Command = (Commands)44;
			GetWMIBatteryHealthmodeControlStatus(val3);
			break;
		}
		case 44:
		{
			List<string> list2 = args.argList.Select((object s) => (string)s).ToList();
			string text7 = list2[0];
			new ManagementObject();
			ManagementScope val8 = null;
			val8 = new ManagementScope("\\root\\wmi");
			val8.Connect();
			WqlEventQuery val9 = new WqlEventQuery();
			val9.EventClassName = "APGeEvent";
			val9.Condition = "";
			if (text7 == "ACEvent")
			{
				ACEventwmiWatcher = new ManagementEventWatcher(val8, (EventQuery)(object)val9);
				ACEventwmiWatcher.EventArrived += WaitForWMIACEvent;
				ACEventwmiWatcher.Start();
			}
			else if (text7 == "CalibrationEvent")
			{
				CalibrationEventwmiWatcher = new ManagementEventWatcher(val8, (EventQuery)(object)val9);
				CalibrationEventwmiWatcher.EventArrived += WaitForCalibrationWMIEvent;
				CalibrationEventwmiWatcher.Start();
			}
			break;
		}
		case 45:
		{
			List<string> list = args.argList.Select((object s) => (string)s).ToList();
			string text4 = list[0];
			Log.i("[ACCStd] cancelRegisterEvent" + text4);
			if (text4 == "CancelACEvent")
			{
				ACEventwmiWatcher.Stop();
				ACEventwmiWatcher.EventArrived -= WaitForWMIACEvent;
				ACEventwmiWatcher = null;
			}
			else if (text4 == "CancelCalibrationEvent")
			{
				CalibrationEventwmiWatcher.Stop();
				CalibrationEventwmiWatcher.EventArrived -= WaitForCalibrationWMIEvent;
				CalibrationEventwmiWatcher = null;
			}
			break;
		}
		case 6:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				string text10 = args.argList[0] as string;
				Log.i("[ACCStd] [CreateHKMKey] start");
				Log.i("[ACCStd] [CreateHKMKey] path: " + text10);
				RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(text10, writable: true);
				if (registryKey == null)
				{
					registryKey = Registry.LocalMachine.CreateSubKey(text10);
				}
				Log.i("[ACCStd] [CreateHKMKey] end");
			}));
			break;
		case 7:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				try
				{
					string text10 = args.argList[0] as string;
					string text11 = args.argList[1] as string;
					object obj5 = args.argList[2];
					bool flag = "true".Equals(args.argList[3].ToString());
					Log.i(string.Concat(new object[8] { "[ACCStd][SetHKLMValue] key:", text10, " targetName:", text11, " value:", obj5, " isBase:", flag }));
					RegistryKey registryKey = null;
					Log.i("[ABB] args.isBase" + flag);
					if (flag)
					{
						registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(text10, writable: true);
					}
					else
					{
						Log.i("regkeybase");
						registryKey = Registry.LocalMachine.OpenSubKey(text10, writable: true);
						Log.i("OpenSubKey regkeybase");
					}
					if (args.Registrytype != RegistryValueKind.Unknown)
					{
						Log.i(string.Concat(new object[8]
						{
							"jay",
							text10,
							" targetName:",
							text11,
							" value:",
							obj5,
							" args.Registrytype:",
							args.Registrytype.ToString()
						}));
						registryKey.SetValue(text11, obj5, args.Registrytype);
						Log.i("finish args.Registrytype");
					}
					else
					{
						Log.i("SetValue");
						registryKey.SetValue(text11, obj5);
						Log.i("finish SetValue");
					}
					Log.i("[ACCStd] [SetHKLMValue] finish");
				}
				catch (Exception ex4)
				{
					Log.e("[ACCStd] SetHKLMValue exception: " + ex4.Message);
					Log.e("[ACCStd] SetHKLMValue exception: " + ex4.ToString());
				}
			}));
			break;
		case 10:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [DeleteHKLMValue] start");
				string name = args.argList[0] as string;
				string name2 = args.argList[1] as string;
				bool flag = "true".Equals(args.argList[2].ToString());
				RegistryKey registryKey = null;
				Log.i("[ABB] args.isBase" + flag);
				registryKey = ((!flag) ? Registry.LocalMachine.OpenSubKey(name, writable: true) : RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(name, writable: true));
				if (registryKey.GetValue(name2) != null)
				{
					registryKey.DeleteValue(name2);
				}
				Log.i("[ACCStd] [DeleteHKLMValue] finish");
			}));
			break;
		case 8:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				string text10 = args.argList[0] as string;
				Log.i("[ACCStd] [CreateCUKey] start");
				Log.i("[ACCStd] [CreateCUKey] path: " + text10);
				using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text10, writable: true))
				{
					if (registryKey == null)
					{
						Registry.CurrentUser.CreateSubKey(text10);
					}
				}
				Log.i("[ACCStd] [CreateCUKey] end");
			}));
			break;
		case 9:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				try
				{
					string name = args.argList[0] as string;
					string name2 = args.argList[1] as string;
					object obj5 = args.argList[2];
					RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name, writable: true);
					try
					{
						switch (args.Registrytype)
						{
						case RegistryValueKind.String:
						{
							string value = (string)obj5;
							registryKey.SetValue(name2, value, RegistryValueKind.String);
							break;
						}
						case RegistryValueKind.DWord:
						{
							int num2 = (int)obj5;
							registryKey.SetValue(name2, num2, RegistryValueKind.DWord);
							break;
						}
						}
					}
					catch (Exception)
					{
					}
					Log.i("[ACCStd] [SetCUValue] finish");
				}
				catch (Exception ex5)
				{
					Log.e("[ACCStd] SetCUValue exception: " + ex5.Message);
					Log.e("[ACCStd] SetCUValue exception: " + ex5.ToString());
				}
			}));
			break;
		case 28:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [DeleteCULMValue] start");
				string name = args.argList[0] as string;
				string name2 = args.argList[1] as string;
				bool flag = "true".Equals(args.argList[2].ToString());
				RegistryKey registryKey = null;
				Log.i("[ABB] args.isBase" + flag);
				registryKey = ((!flag) ? Registry.CurrentUser.OpenSubKey(name, writable: true) : RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(name, writable: true));
				if (registryKey.GetValue(name2) != null)
				{
					registryKey.DeleteValue(name2);
				}
				Log.i("[ACCStd] [DeleteCULMValue] finish");
			}));
			break;
		case 11:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [CreateTask] start");
				string path4 = args.argList[0] as string;
				string text10 = args.argList[1] as string;
				try
				{
					Process process5 = new Process
					{
						StartInfo = 
						{
							FileName = "SCHTASKS.exe",
							WindowStyle = ProcessWindowStyle.Hidden,
							Arguments = "/Create /TN \"" + Path.Combine("CareCenter", path4) + "\" /XML \"" + text10 + "\" /F"
						}
					};
					Log.e("[ACCStd] Task Argument: " + process5.StartInfo.Arguments);
					process5.Start();
					process5.WaitForExit();
					Log.i("[ACCStd] [CreateTask] finish");
				}
				catch (Exception)
				{
					Log.e("[ACCStd] [AddDelayTasksbyStartupType] " + text10 + " can't be added to task scheduler.");
				}
			}));
			break;
		case 12:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [DeleteTask] start");
				string text10 = args.argList[0] as string;
				try
				{
					Process process5 = new Process();
					process5.StartInfo.FileName = "SCHTASKS.exe";
					process5.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					process5.StartInfo.Arguments = "/Delete /TN \"" + Path.Combine("CareCenter", text10) + "\" /F";
					process5.StartInfo.Arguments.Remove(22, 1);
					process5.Start();
					process5.WaitForExit();
					Log.i("[ACCStd] [DeleteTask] finish");
				}
				catch
				{
					Log.e("[ACCStd] [RemoveTask] delete " + text10 + " task fail.");
				}
			}));
			break;
		case 13:
		{
			Thread thread = new Thread(() =>
			{
				Log.i("[ACCStd] [LaunchProc] start");
				string obj5 = args.argList[0] as string;
				bool flag = (bool)args.argList[1];
				try
				{
					Process process5 = new Process
					{
						StartInfo = GetPropertyValues(obj5)
					};
					lock (_pProcLock)
					{
						if (flag)
						{
							Log.d("[ACCStd] [LaunchProc] FixpackTask start");
							process5.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
							string fileName4 = process5.StartInfo.FileName;
							process5.Start();
							process5.WaitForExit();
							int exitCode = process5.ExitCode;
							Log.d("[ACCStd] [LaunchProc] result: " + exitCode);
							writeIni.WriteIni_ForUpdateReturn(Path.GetDirectoryName(fileName4), "ReturnInfo", "Name", process5.StartInfo.FileName);
							writeIni.WriteIni_ForUpdateReturn(Path.GetDirectoryName(fileName4), "ReturnInfo", "ExitCode", exitCode.ToString());
							writeIni.WriteIni_ForUpdateReturn(Path.GetDirectoryName(fileName4), "ReturnInfo", "ExitTime", process5.ExitTime.ToString());
							if (File.Exists(Path.GetDirectoryName(fileName4) + "\\ReturnCodeInfo_finish.ini"))
							{
								File.Delete(Path.GetDirectoryName(fileName4) + "\\ReturnCodeInfo_finish.ini");
							}
							File.Move(Path.GetDirectoryName(fileName4) + "\\ReturnCodeInfo.ini", Path.GetDirectoryName(fileName4) + "\\ReturnCodeInfo_finish.ini");
						}
						else
						{
							Log.d("[ACCStd] [LaunchProc] normal LaunchProcess start");
							process5.Start();
							Thread.Sleep(5000);
							SetForegroundWindow(process5.MainWindowHandle);
							process5.Close();
							Log.d("[ACCStd] [LaunchProc] LaunchProcess.MainWindowHandle : " + process5.MainWindowHandle);
						}
						Log.i("[ACCStd] [LaunchProc] finish");
					}
				}
				catch (Exception ex4)
				{
					Log.e("[ACCStd] [LaunchProc] fail :" + ex4.Message);
				}
			});
			thread.Start();
			break;
		}
		case 14:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				Log.i("[ACCStd] [WMISetFunction_Pipe_Command] start");
				BIOSOption option = (BIOSOption)args.argList[0];
				int state = (int)args.argList[1];
				long errorcode = -1L;
				try
				{
					SetBIOSOption(option, state, ref errorcode);
					Log.i("[ACCStd] [WMISetFunction_Pipe_Command] finish");
				}
				catch
				{
					Log.e("[ACCStd] [WMISetFunction_Pipe_Command] task fail.");
				}
			}));
			break;
		case 63:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				Log.i("[ACCStd] [WMISetFunction_AcerGenericMethod_Pipe_Command] start");
				BIOSOption_AcerGenericMethod option = (BIOSOption_AcerGenericMethod)args.argList[0];
				int state = (int)args.argList[1];
				long errorcode = -1L;
				try
				{
					SetBIOSOption_AcerGenericMethod(option, state, ref errorcode);
					Log.i("[ACCStd] [WMISetFunction_AcerGenericMethod_Pipe_Command] finish");
				}
				catch
				{
					Log.e("[ACCStd] [WMISetFunction_AcerGenericMethod_Pipe_Command] task fail.");
				}
			}));
			break;
		case 15:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [SetWMI_For_ControlPanal_Pipe_Command] start");
				try
				{
					string text10 = args.argList[0] as string;
					string text11 = args.argList[1] as string;
					string text12 = args.argList[2] as string;
					bool flag = (bool)args.argList[3];
					Log.i("[WMISetFunction_Pipe_Command] pnpDeviceID:" + text10 + " item:" + text11 + " prop:" + text12 + " value:" + flag);
					foreach (Adapter adapter in adapters)
					{
						if (text10.Equals(adapter.pnpDeviceID))
						{
							Log.i("[WMISetFunction_Pipe_Command] match PNPID : " + text10);
							if ("WMIWake".Equals(text11))
							{
								ManagementObject wMIWakeObj = adapter.WMIWakeObj;
								PropertyDataCollection properties = ((ManagementBaseObject)wMIWakeObj).Properties;
								properties[text12].Value = flag;
								wMIWakeObj.Put();
								adapter.setWakeManagementObj(wMIWakeObj);
							}
							if ("WMIPower".Equals(text11))
							{
								ManagementObject wMIPowerObj = adapter.WMIPowerObj;
								PropertyDataCollection properties2 = ((ManagementBaseObject)wMIPowerObj).Properties;
								properties2[text12].Value = flag;
								wMIPowerObj.Put();
								adapter.setPowerManagementObj(wMIPowerObj);
							}
						}
					}
					Log.i("[ACCStd] [SetWMI_For_ControlPanal_Pipe_Command] finish");
				}
				catch
				{
					Log.e("[ACCStd] [SetWMI_For_ControlPanal_Pipe_Command] task fail.");
				}
			}));
			break;
		case 16:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [StartUpdateDriveInfo] start");
				StartMonitorDriveInfo_ADS();
				Log.i("[ACCStd] [StartUpdateDriveInfo] finish");
			}));
			break;
		case 17:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				Log.i("[ACCStd] [StopUpdateDriveInfo] start");
				StopMonitorDriveDriveInfo_ADS();
				Log.i("[ACCStd] [StopUpdateDriveInfo] finish");
			}));
			break;
		case 18:
			try
			{
				ReadWriteIni val10 = new ReadWriteIni();
				val10.WriteIni_checkingHDD("Harddisk", "HDDSN", args.argList[0].ToString());
			}
			catch (Exception)
			{
			}
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
				//IL_0200: Expected Obj, but got Unknown
				//IL_014d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0153: Invalid comparison between Unknown and I4
				List<MultiDevicesTests> list3 = new List<MultiDevicesTests>();
				List<string> list4 = new List<string>();
				for (int i = 0; i < args.argList.Count; i++)
				{
					list4.Add(args.argList[i].ToString());
					Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] SN: " + args.argList[i].ToString());
				}
				try
				{
					List<MultiDevicesTests> specificDeviceInfo3 = DiagnosticManager_ACCStd.Instance.GetSpecificDeviceInfo(DiagnosticManager_ACCStd.DeviceType.HardDisk);
					Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] mdTest_temp: " + specificDeviceInfo3.Count);
					DeviceTests val11 = null;
					foreach (MultiDevicesTests item2 in specificDeviceInfo3)
					{
						Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] mditem.m_lDeviceList: " + item2.m_lDeviceList.Count);
						foreach (DeviceTests lDevice in item2.m_lDeviceList)
						{
							Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] dvitem.m_lDeviceInfo: " + lDevice.m_lDeviceInfo.Count);
							foreach (DiagDeviceInfo item3 in lDevice.m_lDeviceInfo)
							{
								Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] deviceInfo.m_PropertyName: " + item3.m_PropertyName);
								Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] deviceInfo.m_value: " + item3.m_value);
								if ((int)item3.m_key == 2)
								{
									foreach (string item4 in list4)
									{
										if (item3.m_value.Equals(item4))
										{
											object obj5 = lDevice.Clone();
											val11 = (DeviceTests)((obj5 is DeviceTests) ? obj5 : null);
										}
									}
								}
							}
						}
						if (val11 != null)
						{
							MultiDevicesTests item = new MultiDevicesTests(new List<DeviceTests> { val11 }, HDD_TYPE, false);
							list3.Add(item);
						}
					}
				}
				catch (Exception ex4)
				{
					Log.e("[ACCStd] [StartTestHardDrive_Pipe_Command] exception: " + ex4.ToString());
				}
				Log.i("[ACCStd] [StartTestHardDrive_Pipe_Command] start");
				if (list3.Count != 0)
				{
					Log.i("[ACCStd] [StartTestHardDrive_Pipe_Command] List<MultiDevicesTests> mdTest count is not 0");
					DiagnosticManager_ACCStd.Instance.StartTest(list3);
				}
				else
				{
					Log.i("[ACCStd] [StartTestHardDrive_Pipe_Command] List<MultiDevicesTests> mdTest count is 0");
				}
				Log.i("[ACCStd] [StartTestHardDrive_Pipe_Command] finish");
			}));
			break;
		case 19:
			if (File.Exists("C:\\ProgramData\\Acer\\CareCenter\\checkingHDD.ini"))
			{
				try
				{
					File.Delete("C:\\ProgramData\\Acer\\CareCenter\\checkingHDD.ini");
				}
				catch
				{
					break;
				}
			}
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
				//IL_01d8: Expected Obj, but got Unknown
				//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f1: Invalid comparison between Unknown and I4
				List<MultiDevicesTests> list3 = new List<MultiDevicesTests>();
				List<string> list4 = new List<string>();
				Log.i("[ACCStd][StopTestHardDrive] args.argList.Count:" + args.argList.Count);
				for (int i = 0; i < args.argList.Count; i++)
				{
					list4.Add(args.argList[i].ToString());
					Log.i("[ACCStd][StopTestHardDrive] HD_SNList:" + args.argList[i].ToString());
				}
				try
				{
					List<MultiDevicesTests> specificDeviceInfo3 = DiagnosticManager_ACCStd.Instance.GetSpecificDeviceInfo(DiagnosticManager_ACCStd.DeviceType.HardDisk);
					foreach (MultiDevicesTests item5 in specificDeviceInfo3)
					{
						DeviceTests val11 = null;
						foreach (DeviceTests lDevice2 in item5.m_lDeviceList)
						{
							foreach (DiagDeviceInfo item6 in lDevice2.m_lDeviceInfo)
							{
								if ((int)item6.m_key == 2)
								{
									Log.i("[ACCStd][StopTestHardDrive] deviceInfo.m_value: " + item6.m_value);
									foreach (string item7 in list4)
									{
										if (item6.m_value.Equals(item7))
										{
											Log.i("[ACCStd][StopTestHardDrive] args.argList.Count: " + args.argList.Count);
											val11 = lDevice2;
										}
									}
								}
							}
						}
						if (val11 != null)
						{
							Log.i("[ACCStd][StopTestHardDrive] keepItem != null");
							MultiDevicesTests item = new MultiDevicesTests(new List<DeviceTests> { val11 }, HDD_TYPE, false);
							list3.Add(item);
						}
					}
				}
				catch (Exception ex4)
				{
					Log.e("[ACCStd] [StopTestHardDrive_Pipe_Command] exception: " + ex4.ToString());
				}
				Log.i("[ACCStd] [StopTestHardDrive_Pipe_Command] start");
				if (list3.Count != 0)
				{
					Log.i("[ACCStd] [StopTestHardDrive_Pipe_Command] List<MultiDevicesTests> mdTest count is not 0");
					DiagnosticManager_ACCStd.Instance.StopTest(list3);
				}
				else
				{
					Log.i("[ACCStd] [StopTestHardDrive_Pipe_Command] List<MultiDevicesTests> mdTest count is 0");
				}
				Log.i("[ACCStd] [StopTestHardDrive_Pipe_Command] finish");
			}));
			break;
		case 61:
		{
			Log.i("[ACCStd] [StartNVMEDrive_Pipe_Command] start");
			Cancel_NVMEDST = false;
			Monitor_Progress();
			Monitor_Result();
			string text9 = "";
			text9 = args.argList[0].ToString();
			Log.i("[ACCStd] storage_index:" + text9);
			string arguments2 = "-D " + text9 + " a ";
			string fileName3 = AppDomain.CurrentDomain.BaseDirectory + NVMEDST_tool;
			List<HardDriveName> _dicHardDriveInfo = new List<HardDriveName>();
			GetDeviceHardDrive(ref _dicHardDriveInfo);
			for (int num = 0; num < _dicHardDriveInfo.Count; num++)
			{
				if (_dicHardDriveInfo[num].Index == text9)
				{
					NVME_SN = _dicHardDriveInfo[num].SN;
					break;
				}
			}
			try
			{
				Process process4 = new Process();
				process4.StartInfo.UseShellExecute = false;
				process4.StartInfo.CreateNoWindow = true;
				process4.StartInfo.RedirectStandardInput = true;
				process4.StartInfo.RedirectStandardOutput = true;
				process4.StartInfo.RedirectStandardError = true;
				process4.StartInfo.FileName = fileName3;
				process4.StartInfo.Arguments = arguments2;
				process4.Start();
				break;
			}
			catch (Exception)
			{
				break;
			}
		}
		case 62:
		{
			Log.i("[ACCStd] [StopNVMEDrive_Pipe_Command] stop");
			Cancel_NVMEDST = true;
			string text8 = "";
			text8 = args.argList[0].ToString();
			Log.i("[ACCStd] storage_index:" + text8);
			string arguments = "-D " + text8 + " b ";
			string fileName2 = AppDomain.CurrentDomain.BaseDirectory + NVMEDST_tool;
			try
			{
				Process process3 = new Process();
				process3.StartInfo.UseShellExecute = false;
				process3.StartInfo.CreateNoWindow = true;
				process3.StartInfo.RedirectStandardInput = true;
				process3.StartInfo.RedirectStandardOutput = true;
				process3.StartInfo.RedirectStandardError = true;
				process3.StartInfo.FileName = fileName2;
				process3.StartInfo.Arguments = arguments;
				process3.Start();
				break;
			}
			catch (Exception ex)
			{
				Log.i("[ACCStd] process error" + ex.ToString());
				break;
			}
		}
		case 51:
		{
			Log.i("[ACCStd] [StartTestMemory_Pipe_Command] start");
			List<MultiDevicesTests> specificDeviceInfo2 = DiagnosticManager_ACCStd_for_Memory.Instance.GetSpecificDeviceInfo(DiagnosticManager_ACCStd_for_Memory.DeviceType.RAM);
			if (specificDeviceInfo2.Count != 0)
			{
				Log.i("[ACCStd] [StartTestMemory_Pipe_Command] List<MultiDevicesTests> mdTest count is not 0");
				DiagnosticManager_ACCStd_for_Memory.Instance.StartTest(specificDeviceInfo2);
			}
			else
			{
				Log.i("[ACCStd] [StartTestHardDrive_Pipe_Command] List<MultiDevicesTests> mdTest count is 0");
			}
			break;
		}
		case 52:
		{
			Log.i("[ACCStd] [StopTestMemory_Pipe_Command] stop");
			List<MultiDevicesTests> specificDeviceInfo = DiagnosticManager_ACCStd_for_Memory.Instance.GetSpecificDeviceInfo(DiagnosticManager_ACCStd_for_Memory.DeviceType.RAM);
			if (specificDeviceInfo.Count != 0)
			{
				Log.i("[ACCStd] [StopTestMemory_Pipe_Command] List<MultiDevicesTests> mdTest count is not 0");
				DiagnosticManager_ACCStd_for_Memory.Instance.StopTest(specificDeviceInfo);
			}
			else
			{
				Log.i("[ACCStd] [StartTestHardDrive_Pipe_Command] List<MultiDevicesTests> mdTest count is 0");
			}
			break;
		}
		case 57:
		{
			Log.i("Jatttt");
			string path2 = "C:\\ProgramData\\Acer\\CareCenter\\HardWare_status.ini";
			string path3 = "C:\\ProgramData\\Acer\\CareCenter\\CareCenterConfig.ini";
			if (!File.Exists(path2) && File.Exists(path3))
			{
				DiagnosticManager_ACCStd.Instance.write_Harddrive_ini();
			}
			break;
		}
		case 58:
		{
			string empty = string.Empty;
			Log.i("[ACCStd][WriteHarddisk_status] args.argList.Count:" + args.argList.Count);
			empty = args.argList[0].ToString();
			DiagnosticManager_ACCStd.Instance.WriteHarddisk_status_Unknown(empty);
			break;
		}
		case 20:
			((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
			{
				//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e3: Expected Obj, but got Unknown
				//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
				//IL_0106: Expected Obj, but got Unknown
				Log.i("[ACCStd] [LaunchDefragFunction_Pipe_Command] start");
				Log.i("[ACCStd] [LaunchDefragFunction_Pipe_Command] FunctionName");
				string obj5 = args.argList[0] as string;
				string text10 = args.argList[1] as string;
				string arguments3 = args.argList[2] as string;
				Log.i("[ACCStd] [LaunchDefragFunction_Pipe_Command] FunctionName: " + text10);
				try
				{
					Process process5 = new Process();
					process5.StartInfo = GetPropertyValues(obj5);
					process5.StartInfo.Arguments = arguments3;
					process5.StartInfo.CreateNoWindow = true;
					process5.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					_ = process5.StartInfo.FileName;
					process5.Start();
					process5.WaitForExit();
					int exitCode = process5.ExitCode;
					Log.i("[ACCStd] [LaunchDefragFunction_Pipe_Command] result: " + exitCode);
					RunningStatusData val11 = new RunningStatusData();
					List<object> list3 = new List<object> { text10 };
					if (list3 != null)
					{
						val11.AddArgs(list3);
					}
					PipeClient val12 = new PipeClient();
					val12.SendMsg(PipeDef._ACCPipeName_DiskDefrag, (object)val11, 10000);
					Log.i("[ACCStd] [LaunchDefragFunction_Pipe_Command] finish");
				}
				catch (Exception ex4)
				{
					Log.e("[ACCStd] [LaunchProc] fail :" + ex4.Message);
				}
			}));
			break;
		case 21:
			CalJunkFileSize();
			break;
		case 22:
		{
			bool chkRecycle = (bool)args.argList[0];
			bool chkTempFiles = (bool)args.argList[1];
			bool chkBrowerCache = (bool)args.argList[2];
			Clean(chkRecycle, chkTempFiles, chkBrowerCache);
			RunningStatusData val5 = new RunningStatusData();
			PipeClient val6 = new PipeClient();
			val6.SendMsg(PipeDef._ACCPipeName_JunKFileControl, (object)val5, 10000);
			break;
		}
		case 23:
		{
			string fileName = args.argList[0] as string;
			string verb = args.argList[1] as string;
			Process process2 = new Process();
			process2.StartInfo.FileName = fileName;
			process2.StartInfo.Verb = verb;
			process2.Start();
			break;
		}
		case 59:
		{
			string text5 = args.argList[0] as string;
			string text6 = args.argList[1] as string;
			Log.i("Jay_test  iniPath:" + text5);
			Log.i("Jay_test  _newiniPath:" + text6);
			try
			{
				File.SetAttributes(text5, FileAttributes.Normal);
				File.Move(text5, text6);
				break;
			}
			catch
			{
				break;
			}
		}
		case 24:
		{
			string path = args.argList[0] as string;
			try
			{
				File.SetAttributes(path, FileAttributes.Normal);
				File.Delete(path);
				break;
			}
			catch
			{
				break;
			}
		}
		case 60:
		{
			string lpAppName = args.argList[0] as string;
			string lpKeyName = args.argList[1] as string;
			string lpString = args.argList[2] as string;
			string lpFileName = args.argList[3] as string;
			WritePrivateProfileString(lpAppName, lpKeyName, lpString, lpFileName);
			break;
		}
		case 26:
		{
			Log.i("[ACCStd] [Check_Update_For_Background] start");
			string text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OEM\\UpgradeTool\\CareCenter_v4\\UpgradeTool.exe");
			if ((int)CheckEnvironment.Instance.CheckModelType() == 1)
			{
				text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OEM\\UpgradeTool\\ControlCenter_v4\\UpgradeTool.exe");
			}
			else if ((int)CheckEnvironment.Instance.CheckModelType() == 0)
			{
				text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OEM\\UpgradeTool\\CareCenter_v4\\UpgradeTool.exe");
			}
			Log.i("[ACCStd] [Check_Update_For_Background] upgradeToolPath : " + text3);
			if (File.Exists(text3))
			{
				Process process = new Process();
				process.StartInfo.FileName = text3;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.Arguments = "-ns";
				process.Start();
				process.WaitForExit();
				Log.i("[ACCStd] [Check_Update_For_Background] ExitCode : " + process.ExitCode);
				if (process.ExitCode != 1124 && process.ExitCode != 1125)
				{
					UpdaterEnvironment.LastSelfUpdateTime = DateTime.Now;
				}
			}
			Log.e("[ACCStd] [Check_Update_For_Background] end");
			break;
		}
		case 27:
			GetXMLStartupInfo();
			break;
		case 29:
		{
			string text = args.argList[0] as string;
			string text2 = args.argList[1] as string;
			NotifyPopup val2 = new NotifyPopup();
			val2.initial_Balloon(CommItem._AppIcon);
			val2.initial_Toast();
			val2.ToastClick += NPI_ToastClick;
			val2.BalloonClick += NPI_BalloonClick;
			if ((int)CheckEnvironment.Instance.CheckModelType() == 1)
			{
				val2.show(text, text2, CheckEnvironment.appIDorPath_ACtC);
			}
			else
			{
				val2.show(text, text2, CheckEnvironment.appIDorPath_ACC);
			}
			break;
		}
		case 31:
			Log.d("Add_HistoryList_Pipe_Command");
			ADD_HistoryList(args.argList);
			break;
		case 32:
			Log.d("DeleteOne_HistoryList_Pipe_Command");
			DeleteOne_HistoryList(args.argList);
			break;
		case 33:
			Log.d("DeleteAll_HistoryList_Pipe_Command");
			DeleteAll_HistoryList();
			break;
		case 50:
		{
			string target_dir = args.argList[0] as string;
			DeleteDirectory(target_dir);
			break;
		}
		case 53:
			Launch_ACCUI();
			break;
		case 55:
			if (File.Exists("C:\\ProgramData\\Acer\\CareCenter\\checkingHDD.ini"))
			{
				try
				{
					File.Delete("C:\\ProgramData\\Acer\\CareCenter\\checkingHDD.ini");
					break;
				}
				catch
				{
					break;
				}
			}
			break;
		case 0:
		case 5:
		case 25:
		case 30:
		case 35:
		case 36:
		case 37:
		case 42:
		case 43:
		case 46:
		case 48:
		case 49:
		case 54:
		case 56:
			break;
		}
	}

	public static void DeleteDirectory(string target_dir)
	{
		string[] files = Directory.GetFiles(target_dir);
		string[] directories = Directory.GetDirectories(target_dir);
		string[] array = files;
		foreach (string path in array)
		{
			File.SetAttributes(path, FileAttributes.Normal);
			File.Delete(path);
		}
		string[] array2 = directories;
		foreach (string target_dir2 in array2)
		{
			DeleteDirectory(target_dir2);
		}
		Directory.Delete(target_dir, recursive: false);
	}

	private void WaitForWMIACEvent(object sender, EventArrivedEventArgs e)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected Obj, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected Obj, but got Unknown
		try
		{
			WMIEvent = (byte[])e.NewEvent.Properties["EventDetail"].Value;
			if (WMIEvent[0] == 8)
			{
				TransferData val = new TransferData();
				val.Command = (Commands)47;
				List<object> list = new List<object>();
				if (WMIEvent[1] == 0)
				{
					list.Add("False");
				}
				else if (WMIEvent[1] == 1)
				{
					list.Add("True");
				}
				val.argList = list;
				PipeClient val2 = new PipeClient();
				val2.SendMsg(PipeDef._ACCPipeName_ChecksupportHealthcontrol, (object)val, 10000);
			}
		}
		catch
		{
		}
	}

	private void WaitForCalibrationWMIEvent(object sender, EventArrivedEventArgs e)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected Obj, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected Obj, but got Unknown
		WMIEvent = (byte[])e.NewEvent.Properties["EventDetail"].Value;
		if (WMIEvent[0] == 11)
		{
			TransferData val = new TransferData();
			val.Command = (Commands)48;
			List<object> list = new List<object>();
			if (WMIEvent[1] == 0)
			{
				list.Add("False");
			}
			else if (WMIEvent[1] == 1)
			{
				list.Add("True");
			}
			val.argList = list;
			PipeClient val2 = new PipeClient();
			val2.SendMsg(PipeDef._ACCPipeName_ChecksupportHealthcontrol, (object)val, 10000);
		}
	}

	private void GetWMIBatteryHealthControlStatus()
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected Obj, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected Obj, but got Unknown
		parameterBatteryhealthControl parameterBatteryhealthControl2 = default;
		parameterBatteryhealthControl2.intialize(255u, 1, 255, 255, a5: false, a6: false);
		bool batteryHealthControlStatus = wmimanagementwmi.GetBatteryHealthControlStatus(1u, parameterBatteryhealthControl2.SupportQuery, ref parameterBatteryhealthControl2.output, ref parameterBatteryhealthControl2.HealthMode, ref parameterBatteryhealthControl2.CalibrationMode, ref parameterBatteryhealthControl2.State_HealthMode, ref parameterBatteryhealthControl2.State_CalibrationMode);
		TransferData val = new TransferData();
		val.Command = (Commands)36;
		List<object> list = new List<object>();
		list.Add(batteryHealthControlStatus.ToString());
		list.Add(parameterBatteryhealthControl2.HealthMode.ToString());
		list.Add(parameterBatteryhealthControl2.CalibrationMode.ToString());
		list.Add(parameterBatteryhealthControl2.State_HealthMode.ToString());
		list.Add(parameterBatteryhealthControl2.State_CalibrationMode.ToString());
		val.argList = list;
		PipeClient val2 = new PipeClient();
		val2.SendMsg(PipeDef._ACCPipeName_ChecksupportHealthcontrol, (object)val, 10000);
	}

	private void GetWMIBatteryHealthmodeControlStatus(TransferData DataForSend)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected Obj, but got Unknown
		parameterBatteryhealthControl parameterBatteryhealthControl2 = default;
		parameterBatteryhealthControl2.intialize(255u, 1, 255, 255, a5: false, a6: false);
		bool batteryHealthControlStatus = wmimanagementwmi.GetBatteryHealthControlStatus(1u, parameterBatteryhealthControl2.SupportQuery, ref parameterBatteryhealthControl2.output, ref parameterBatteryhealthControl2.HealthMode, ref parameterBatteryhealthControl2.CalibrationMode, ref parameterBatteryhealthControl2.State_HealthMode, ref parameterBatteryhealthControl2.State_CalibrationMode);
		Log.i("[ACCStd] GetBatteryHealthControlStatus");
		List<object> list = new List<object>();
		if (batteryHealthControlStatus && parameterBatteryhealthControl2.output == 0)
		{
			list.Add("True");
		}
		else
		{
			list.Add("False");
		}
		DataForSend.argList = list;
		PipeClient val = new PipeClient();
		val.SendMsg(PipeDef._ACCPipeName_ChecksupportHealthcontrol, (object)DataForSend, 10000);
	}

	private void GetWMIBatteryCalibrationStatus()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected Obj, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected Obj, but got Unknown
		parameterBatteryhealthControl parameterBatteryhealthControl2 = default;
		parameterBatteryhealthControl2.intialize(255u, 1, 255, 255, a5: false, a6: false);
		bool batteryHealthControlStatus = wmimanagementwmi.GetBatteryHealthControlStatus(1u, parameterBatteryhealthControl2.SupportQuery, ref parameterBatteryhealthControl2.output, ref parameterBatteryhealthControl2.HealthMode, ref parameterBatteryhealthControl2.CalibrationMode, ref parameterBatteryhealthControl2.State_HealthMode, ref parameterBatteryhealthControl2.State_CalibrationMode);
		Log.i("[ACCStd] call WMI BatteryHealthControlStatus");
		TransferData val = new TransferData();
		val.Command = (Commands)50;
		List<object> list = new List<object>();
		list.Add(batteryHealthControlStatus.ToString());
		list.Add(parameterBatteryhealthControl2.HealthMode.ToString());
		list.Add(parameterBatteryhealthControl2.CalibrationMode.ToString());
		list.Add(parameterBatteryhealthControl2.State_HealthMode.ToString());
		list.Add(parameterBatteryhealthControl2.State_CalibrationMode.ToString());
		val.argList = list;
		PipeClient val2 = new PipeClient();
		val2.SendMsg(PipeDef._ACCPipeName_ChecksupportHealthcontrol, (object)val, 10000);
	}

	private void SetWMIBatteryHealthControl(int HealthMode, bool State_HealtMode)
	{
		uint num = 255u;
		wmimanagementwmi.WMISetBatteryHealthControlstatus(1u, HealthMode, State_HealtMode, ref num);
	}

	private void Read_iniCalibrationKey()
	{
		string path = "C:\\ProgramData\\Acer\\CareCenter\\CriticalBatteryLevel.ini";
		string text = "";
		string text2 = "";
		string text3 = "";
		string text4 = "";
		string text5 = "";
		string text6 = "";
		string text7 = "";
		string text8 = "";
		string text9 = "";
		string text10 = "";
		string text11 = "";
		string text12 = "";
		if (File.Exists(path))
		{
			writeIni.GetCriticalBatteryLevel(ref text, "DC_CriticalBatteryAction_value");
			writeIni.GetCriticalBatteryLevel(ref text3, "DC_CriticalBatterylevel_value");
			writeIni.GetCriticalBatteryLevel(ref text5, "DC_ReserveBatterylevel_value");
			writeIni.GetCriticalBatteryLevel(ref text7, "DC_Hibernateafter_value");
			writeIni.GetCriticalBatteryLevel(ref text9, "DC_Sleepafter_value");
			writeIni.GetCriticalBatteryLevel(ref text11, "DC_TurnOffDisplayAfter_value");
			writeIni.GetCriticalBatteryLevel(ref text2, "AC_CriticalBatteryAction_value");
			writeIni.GetCriticalBatteryLevel(ref text4, "AC_CriticalBatterylevel_value");
			writeIni.GetCriticalBatteryLevel(ref text6, "AC_ReserveBatterylevel_value");
			writeIni.GetCriticalBatteryLevel(ref text8, "AC_Hibernateafter_value");
			writeIni.GetCriticalBatteryLevel(ref text10, "AC_Sleepafter_value");
			writeIni.GetCriticalBatteryLevel(ref text12, "AC_TurnOffDisplayAfter_value");
			string g = "637ea02f-bbcb-4015-8e2c-a1c7b9c0b546";
			string g2 = "9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469";
			string g3 = "f3c5027d-cd16-4930-aa6b-90db844a8f00";
			string g4 = "9d7815a6-7ee4-497e-8888-515a05f02364";
			string g5 = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";
			string g6 = "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e";
			Guid guid = new Guid("e73a048d-bf27-4f12-9731-8b2076e8891f");
			Guid guid2 = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
			Guid guid3 = new Guid("7516b95f-f776-4464-8c53-06167f40cc99");
			try
			{
				Guid guid4 = new Guid(g);
				Guid guid5 = new Guid(g2);
				Guid guid6 = new Guid(g3);
				Guid guid7 = new Guid(g4);
				Guid guid8 = new Guid(g5);
				Guid guid9 = new Guid(g6);
				IntPtr intPtr = default;
				NativeMethods.PowerGetActiveScheme(IntPtr.Zero, ref intPtr);
				Guid guid10 = (Guid)Marshal.PtrToStructure(intPtr, typeof(Guid));
				NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid10, ref guid, ref guid4, Convert.ToUInt32(text.ToString()));
				NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid10, ref guid, ref guid5, Convert.ToUInt32(text3.ToString()));
				NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid10, ref guid, ref guid6, Convert.ToUInt32(text5.ToString()));
				NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid10, ref guid2, ref guid7, Convert.ToUInt32(text7.ToString()));
				NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid10, ref guid2, ref guid8, Convert.ToUInt32(text9.ToString()));
				NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref guid10, ref guid3, ref guid9, Convert.ToUInt32(text11.ToString()));
				NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid10, ref guid, ref guid4, Convert.ToUInt32(text2.ToString()));
				NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid10, ref guid, ref guid5, Convert.ToUInt32(text4.ToString()));
				NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid10, ref guid, ref guid6, Convert.ToUInt32(text6.ToString()));
				NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid10, ref guid2, ref guid7, Convert.ToUInt32(text8.ToString()));
				NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid10, ref guid2, ref guid8, Convert.ToUInt32(text10.ToString()));
				NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref guid10, ref guid3, ref guid9, Convert.ToUInt32(text12.ToString()));
				NativeMethods.PowerSetActiveScheme(IntPtr.Zero, ref guid10);
				Marshal.FreeHGlobal(intPtr);
			}
			catch (Exception)
			{
			}
			try
			{
				File.Delete("C:\\ProgramData\\Acer\\CareCenter\\CriticalBatteryLevel.ini");
			}
			catch (IOException)
			{
			}
		}
	}

	private void check_DST_ini()
	{
		object obj = new object();
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string text = baseDirectory + DST_ProgressIni;
		string text2 = baseDirectory + DST_ResultIni;
		try
		{
			if (File.Exists(text))
			{
				lock (obj)
				{
					WritePrivateProfileString("Progress", "Progress", "NULL", text);
				}
			}
			if (File.Exists(text2))
			{
				lock (obj)
				{
					WritePrivateProfileString("Result", "Result", "NULL", text2);
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Log.e("[AccStd] check_DST_ini Error " + ex.Message);
		}
	}

	private void Support_NVME_DST()
	{
		List<HardDriveName> _dicHardDriveInfo = new List<HardDriveName>();
		GetDeviceHardDrive(ref _dicHardDriveInfo);
		string fileName = AppDomain.CurrentDomain.BaseDirectory + NVMEDST_tool;
		string lpFileName = "C:\\ProgramData\\Acer\\CareCenter\\StorageSupport_NVMEDST.ini";
		int num = 1;
		for (int i = 0; i < _dicHardDriveInfo.Count; i++)
		{
			if (!(_dicHardDriveInfo[i].HardDriveType != "USB") || !(_dicHardDriveInfo[i].MediaType != "Removable Media"))
			{
				continue;
			}
			string text = "-Q " + _dicHardDriveInfo[i].Index;
			try
			{
				Process process = new Process();
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.FileName = fileName;
				process.StartInfo.Arguments = text;
				process.Start();
				string text2 = process.StandardOutput.ReadToEnd();
				string[] array = text2.Split(new string[1] { Environment.NewLine }, StringSplitOptions.None);
				process.WaitForExit();
				process.Close();
				for (int j = 0; j < array.Length; j++)
				{
					if (!array[j].ToLower().Contains("|supported"))
					{
						continue;
					}
					Log.e("[ACCStd] arg :" + text);
					Log.e("[ACCStd] Support_NVME_DST :" + _dicHardDriveInfo[i].HardModelName);
					WritePrivateProfileString("Support_NVME_DST", "HDD" + num, _dicHardDriveInfo[i].HardModelName, lpFileName);
					WritePrivateProfileString("Support_NVME_DST", "HDDSN" + num, _dicHardDriveInfo[i].SN, lpFileName);
					num++;
					string text3 = AppDomain.CurrentDomain.BaseDirectory + DST_ProgressIni;
					string text4 = AppDomain.CurrentDomain.BaseDirectory + DST_ResultIni;
					try
					{
						if (!File.Exists(text3))
						{
							WritePrivateProfileString("Progress", "Progress", "NULL", text3);
						}
						if (!File.Exists(text4))
						{
							WritePrivateProfileString("Result", "Result", "NULL", text4);
						}
					}
					catch (Exception ex)
					{
						Log.e("[AccStd] Write ini Error " + ex.Message);
					}
				}
			}
			catch (Exception ex2)
			{
				Log.e("Support_NVME_DST error:", ex2);
			}
		}
	}

	private void Monitor_Progress()
	{
		watcher = new FileSystemWatcher();
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		watcher.Path = Path.GetDirectoryName(baseDirectory);
		watcher.IncludeSubdirectories = false;
		watcher.EnableRaisingEvents = true;
		watcher.Filter = DST_ProgressIni;
		watcher.Changed += OnRenamed_Progress;
		watcher.EnableRaisingEvents = true;
	}

	private void Monitor_Result()
	{
		watcher_DST_result = new FileSystemWatcher();
		watcher_DST_result.Path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
		watcher_DST_result.IncludeSubdirectories = false;
		watcher_DST_result.EnableRaisingEvents = true;
		watcher_DST_result.Filter = DST_ResultIni;
		watcher_DST_result.Changed += OnRenamed_Result;
		watcher_DST_result.EnableRaisingEvents = true;
	}

	private void OnRenamed_Progress(object source, FileSystemEventArgs e)
	{
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string filePath = baseDirectory + DST_ProgressIni;
		StringBuilder stringBuilder = new StringBuilder(255);
		GetPrivateProfileString("Progress", "Progress", "NULL", stringBuilder, 255, filePath);
		string text = stringBuilder.ToString();
		if (!(text != "NULL"))
		{
			return;
		}
		try
		{
			Log.e("[AccStd] NVME progress: " + text);
			float num = float.Parse(text);
			if (num != 100f)
			{
				Update_NVMEDST_StatusToUI(num);
			}
		}
		catch (Exception ex)
		{
			Log.e("Support_NVME_DST error:", ex);
		}
	}

	private void OnRenamed_Result(object source, FileSystemEventArgs e)
	{
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string filePath = baseDirectory + DST_ResultIni;
		StringBuilder stringBuilder = new StringBuilder(255);
		GetPrivateProfileString("Result", "Result", "NULL", stringBuilder, 255, filePath);
		string text = stringBuilder.ToString();
		if (!(text != "NULL"))
		{
			return;
		}
		Log.e("[AccStd] NVME result: " + text);
		if (!Cancel_NVMEDST)
		{
			switch (text)
			{
			case "0":
				DiagnosticManager_ACCStd.Instance.RecordTestResult("HardDisk", NVME_SN, (TestResult)2, DateTime.Now);
				break;
			case "1":
			case "2":
				DiagnosticManager_ACCStd.Instance.RecordTestResult("HardDisk", NVME_SN, (TestResult)6, DateTime.Now);
				break;
			case "3":
				Update_NVMEDST_StatusToUI(-1f);
				break;
			}
		}
		Update_NVMEDST_StatusToUI(100f);
		watcher.Changed -= OnRenamed_Progress;
		watcher_DST_result.Changed -= OnRenamed_Result;
	}

	private void Check_Storage_list()
	{
		string list = "Storage_whitelist.xml";
		string list2 = "Storage_blacklist.xml";
		Remove_Storagelist(list);
		Remove_Storagelist(list2);
		string text = "";
		try
		{
			HardwareDeviceManager.Get_WMI_Info("SELECT * FROM Win32_ComputerSystem", "Model", ref text);
		}
		catch (Exception ex)
		{
			Log.e("[AccStd] Get_WMI_Info Error " + ex.Message);
		}
		Log.i("[AccStd] system model name:" + text);
		string text2 = "SOFTWARE\\OEM\\Hiding_Storage";
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(text2, writable: true);
			if (registryKey != null)
			{
				List<string> storagelist = GetStoragelist(text, list);
				Log.i("[AccStd] Whiteliststorage count:" + storagelist.Count);
				string[] valueNames = registryKey.GetValueNames();
				foreach (string text3 in valueNames)
				{
					if (storagelist.Count == 0)
					{
						continue;
					}
					for (int j = 0; j < storagelist.Count; j++)
					{
						if (storagelist[j].Equals(text3))
						{
							Log.i("[AccStd] Delete RigistryKey:" + text3);
							registryKey.DeleteValue(text3);
						}
					}
				}
				string[] valueNames2 = registryKey.GetValueNames();
				if (valueNames2.Length == 0)
				{
					Registry.LocalMachine.DeleteSubKeyTree(text2);
				}
				return;
			}
			List<string> storagelist2 = GetStoragelist(text, list2);
			Log.i("[AccStd] blackliststorage count:" + storagelist2.Count);
			if (storagelist2.Count != 0)
			{
				RegistryKey registryKey2 = Registry.LocalMachine.CreateSubKey(text2);
				for (int k = 0; k < storagelist2.Count; k++)
				{
					registryKey2.SetValue(storagelist2[k], "");
					Log.i("[AccStd] Create RigistryKey:" + storagelist2[k]);
				}
			}
		}
		catch (Exception ex2)
		{
			Log.e("[AccStd] Check_Storage_list Error " + ex2.Message);
		}
	}

	private void Update_NVMEDST_StatusToUI(float progress)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected Obj, but got Unknown
		RunningStatusData val = new RunningStatusData();
		List<object> list = new List<object>();
		list.Add(progress);
		if (list != null)
		{
			val.AddArgs(list);
		}
		PipeClient val2 = new PipeClient();
		val2.SendMsg(PipeDef._ACCPipeName_Checkup_HardDrive, (object)val, 10000);
	}

	private List<string> GetStoragelist(string sys_ModelName, string list)
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		currentDirectory = "C:\\ProgramData\\Acer\\CareCenter\\" + list;
		List<string> list2 = new List<string>();
		XmlDocument xmlDocument = new XmlDocument();
		try
		{
			if (File.Exists(currentDirectory))
			{
				using (Stream inStream = File.OpenRead(currentDirectory))
				{
					xmlDocument.Load(inStream);
				}
				XmlNodeList childNodes = xmlDocument.SelectSingleNode("/Storage/project").ChildNodes;
				bool flag = false;
				foreach (XmlNode item in childNodes)
				{
					XmlElement xmlElement = (XmlElement)item;
					string attribute = xmlElement.GetAttribute("System_model");
					string attribute2 = xmlElement.GetAttribute("Storage");
					if (sys_ModelName.Equals(attribute))
					{
						Log.i("[Storage list]sysModelName: " + attribute + " Storage:" + attribute2);
						flag = true;
						list2.Add(attribute2);
					}
				}
				if (!flag)
				{
					return list2;
				}
				return list2;
			}
			return list2;
		}
		catch
		{
			return list2;
		}
	}

	private void Remove_Storagelist(string list)
	{
		string currentDirectory = Directory.GetCurrentDirectory();
		currentDirectory = currentDirectory + "\\" + list;
		string destFileName = "C:\\ProgramData\\Acer\\CareCenter\\" + list;
		try
		{
			if (File.Exists(currentDirectory))
			{
				File.Copy(currentDirectory, destFileName, overwrite: true);
				File.Delete(currentDirectory);
			}
		}
		catch (IOException ex)
		{
			Log.e("IO Error" + ex.Message);
		}
	}

	private void ADD_HistoryList(List<object> args)
	{
		string text = "C:\\ProgramData\\Acer\\CareCenter\\Notification\\notif_history.xml";
		string path = "C:\\ProgramData\\Acer\\CareCenter\\Notification\\";
		List<string> list = args.Select((object s) => (string)s).ToList();
		XmlDocument xmlDocument = new XmlDocument();
		lock (xmlDocument)
		{
			if (File.Exists(text))
			{
				xmlDocument.Load(text);
				XmlNode xmlNode = xmlDocument.SelectSingleNode("Notification/History");
				XmlElement xmlElement = xmlDocument.CreateElement("List");
				xmlElement.SetAttribute("Title", list[0]);
				xmlElement.SetAttribute("Description", list[1]);
				xmlElement.SetAttribute("Plugin", list[2]);
				xmlElement.SetAttribute("Icon", list[3]);
				xmlElement.SetAttribute("CanClick", list[4]);
				xmlElement.SetAttribute("Time", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
				xmlNode.AppendChild(xmlElement);
				xmlDocument.Save(text);
			}
			else
			{
				Directory.CreateDirectory(path);
				XmlElement xmlElement2 = xmlDocument.CreateElement("Notification");
				xmlDocument.AppendChild(xmlElement2);
				XmlElement xmlElement3 = xmlDocument.CreateElement("History");
				xmlElement2.AppendChild(xmlElement3);
				XmlElement xmlElement4 = xmlDocument.CreateElement("List");
				xmlElement4.SetAttribute("Title", list[0]);
				xmlElement4.SetAttribute("Description", list[1]);
				xmlElement4.SetAttribute("Plugin", list[2]);
				xmlElement4.SetAttribute("Icon", list[3]);
				xmlElement4.SetAttribute("CanClick", list[4]);
				xmlElement4.SetAttribute("Time", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
				xmlElement3.AppendChild(xmlElement4);
				xmlDocument.Save(text);
			}
		}
	}

	private void DeleteOne_HistoryList(List<object> args)
	{
		string filename = "C:\\ProgramData\\Acer\\CareCenter\\Notification\\notif_history.xml";
		List<string> list = args.Select((object s) => (string)s).ToList();
		int num = int.Parse(list[0]);
		Log.d("tagNo:" + num);
		XmlDocument _doc = new XmlDocument();
		try
		{
			Log.d("Deleteone");
			XmlNode node = getNode(ref _doc);
			node.RemoveChild(node.ChildNodes[num - 1]);
			_doc.Save(filename);
		}
		catch (Exception ex)
		{
			Log.e("[NotifyHistory] Remove_Notify(tagNo) " + ex.Message);
		}
	}

	private void DeleteAll_HistoryList()
	{
		string filename = "C:\\ProgramData\\Acer\\CareCenter\\Notification\\notif_history.xml";
		Log.d("xmlNode.RemoveAll");
		XmlDocument _doc = new XmlDocument();
		try
		{
			Log.d("DeleteALL_HistoryList");
			XmlNode node = getNode(ref _doc);
			node.RemoveAll();
			_doc.Save(filename);
		}
		catch (Exception ex)
		{
			Log.e("[NotifyHistory] Remove_Notify() " + ex.Message);
		}
	}

	private XmlNode getNode(ref XmlDocument _doc)
	{
		string text = "C:\\ProgramData\\Acer\\CareCenter\\Notification\\notif_history.xml";
		string path = "C:\\ProgramData\\Acer\\CareCenter\\Notification\\";
		try
		{
			Log.i("[NotifyHistory][getNode] fileName :" + text);
			if (!File.Exists(text))
			{
				Directory.CreateDirectory(path);
				XmlElement xmlElement = _doc.CreateElement("Notification");
				_doc.AppendChild(xmlElement);
				XmlElement newChild = _doc.CreateElement("History");
				xmlElement.AppendChild(newChild);
				_doc.Save(text);
			}
			_doc.Load(text);
		}
		catch (Exception ex)
		{
			Log.e("[NotifyHistory] getNode()" + ex.Message);
			XmlElement xmlElement2 = _doc.CreateElement("Notification");
			_doc.AppendChild(xmlElement2);
			XmlElement newChild2 = _doc.CreateElement("History");
			xmlElement2.AppendChild(newChild2);
			_doc.Save(text);
		}
		return _doc.SelectSingleNode("/Notification/History");
	}

	private void NPI_BalloonClick(object sender, EventArgs e)
	{
		Launch_ACCUI();
	}

	private void NPI_ToastClick(object sender, EventArgs e)
	{
		Log.i("Notify _ToastClick");
		Launch_ACCUI();
	}

	private void Launch_ACCUI()
	{
		try
		{
			string text = CheckEnvironment.Instance.GetInstallPath() + "\\ACCAPPLauncher.exe";
			if (File.Exists(text))
			{
				Log.i("[APP_Launch] APP_Launch exist is :" + text);
				NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "treadstone_service_LightMode", PipeDirection.InOut);
				namedPipeClientStream.Connect();
				IPCMethods.SendCommandByNamedPipe(namedPipeClientStream, 7, new object[1] { 102501 });
				namedPipeClientStream.WaitForPipeDrain();
				namedPipeClientStream.Close();
				Log.i("[APP_Launch] APP_Launch be lauched succeddful!");
			}
		}
		catch (Exception)
		{
		}
	}

	private void delayStart_Elapsed(object sender, ElapsedEventArgs e)
	{
		System.Timers.Timer timer = sender as System.Timers.Timer;
		timer.Stop();
		Thread thread = new Thread(InitialMonitorFramework);
		thread.Start();
	}

	private void InitialMonitorFramework()
	{
		MonitorManager.Instance.LoadPlugins(_pluginFolderName);
		MonitorManager.Instance.LaunchManager();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		Log.i("[ACCStd] Window_Closing Start");
		StopMonitorSelfUpgrade();
		release_icon();
		MonitorManager.Instance.StopAllMonitors();
		Log.i("[ACCStd] Window_Closing End");
	}

	public void initial_icon(Stream path)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected Obj, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected Obj, but got Unknown
		nIcon = new NotifyIcon();
		CommItem._AppIcon = nIcon;
		nIcon.Text = _appName;
		try
		{
			nIcon.Icon = new Icon(path, new Size(24, 24));
		}
		catch (Exception ex)
		{
			Log.e("[AccStd] initial_icon() " + ex.Message);
		}
		nIcon.Visible = true;
		buildIconMenu();
		nIcon.MouseClick += DetectLeft_click;
	}

	private void buildIconMenu()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected Obj, but got Unknown
		ContextMenu val = new ContextMenu();
		MenuItem[] array = new MenuItem[3];
		string text = ((Application.Current.Resources[(object)"MUI_OPEN_APP"] is string text2) ? text2.Replace("%ProductName%", _appName) : ("Open " + _appName));
		array[0] = new MenuItem();
		array[0].Index = 0;
		array[0].Text = text;
		array[0].Click += showACC_Click;
		((Menu)val).MenuItems.Add(array[0]);
		nIcon.ContextMenu = val;
	}

	private void IconChange(Stream path)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected Obj, but got Unknown
		try
		{
			nIcon.Icon = new Icon(path, new Size(24, 24));
		}
		catch (Exception ex)
		{
			Log.e("[AccStd] IconChange() " + ex.Message);
		}
	}

	private void DetectLeft_click(object sender, MouseEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if ((int)e.Button == 1048576)
		{
			showACC_Click(sender, (EventArgs)(object)e);
		}
	}

	private void showACC_Click(object sender, EventArgs e)
	{
		Log.i("Click TranIcon ACCStd and launch ACCUI");
		Launch_ACCUI();
	}

	public void release_icon()
	{
		((Component)(object)nIcon).Dispose();
		if (_timeForDriveChange != null)
		{
			_timeForDriveChange.Dispose();
		}
	}

	private void Command_LaunchACC(TransferData args)
	{
		string mainProgramNameACC = _mainProgramNameACC;
		mainProgramNameACC = ((!File.Exists(_mainProgramNameACC)) ? _mainProgramNameACtC : _mainProgramNameACC);
		ProcessStartInfo processStartInfo = new ProcessStartInfo(mainProgramNameACC);
		string text = string.Empty;
		if (args.argList.Count != 0)
		{
			foreach (object arg in args.argList)
			{
				text = text + (string)arg + " ";
			}
			processStartInfo.Arguments = text.Trim();
		}
		Process.Start(processStartInfo);
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		TransferData val = new TransferData();
		val.MonitorName = ((object)(MonitorObjectNames)0).ToString();
		val.PlugName = ((object)(Plugins)3).ToString();
		val.Command = (Commands)3;
		_pipeServer_PipeMessage(val);
	}

	private void Stop_Click(object sender, RoutedEventArgs e)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		TransferData val = new TransferData();
		val.MonitorName = ((object)(MonitorObjectNames)0).ToString();
		val.PlugName = ((object)(Plugins)3).ToString();
		val.Command = (Commands)4;
		_pipeServer_PipeMessage(val);
	}

	private void Update_Click(object sender, RoutedEventArgs e)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected Obj, but got Unknown
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected Obj, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		TransferData val = new TransferData();
		ObjectValue item = new ObjectValue("NotifyInterval_second", (object)5000, (RegistryValueType)1);
		ObjectValue item2 = new ObjectValue("NotifyPercent", (object)95, (RegistryValueType)1);
		val.argList = new List<object>();
		val.argList.Add(item);
		val.argList.Add(item2);
		val.MonitorName = ((object)(MonitorObjectNames)0).ToString();
		val.PlugName = ((object)(Plugins)3).ToString();
		val.Command = (Commands)2;
		_pipeServer_PipeMessage(val);
	}

	private void Initial_Click(object sender, RoutedEventArgs e)
	{
		InitialMonitorFramework();
	}

	private bool CheckProcessExist(string ProcName)
	{
		return Process.GetProcessesByName(ProcName).Length > 0;
	}

	private ProcessStartInfo GetPropertyValues(object obj)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo();
		string text = obj as string;
		string[] array = text.Split(new char[1] { ';' });
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string[] array3 = text2.Split(new char[1] { '=' });
			if (array3.Length == 2)
			{
				string text3 = array3[0];
				string text4 = array3[1];
				Log.e("[AccStd] propertyName: " + text3 + " value: " + text4);
				switch (text3)
				{
				case "CreateNoWindow":
					processStartInfo.CreateNoWindow = "True".Equals(text4);
					break;
				case "RedirectStandardInput":
					processStartInfo.RedirectStandardInput = "True".Equals(text4);
					break;
				case "RedirectStandardOutput":
					processStartInfo.RedirectStandardOutput = "True".Equals(text4);
					break;
				case "RedirectStandardError":
					processStartInfo.RedirectStandardError = "True".Equals(text4);
					break;
				case "UseShellExecute":
					processStartInfo.UseShellExecute = "True".Equals(text4);
					break;
				case "LoadUserProfile":
					processStartInfo.LoadUserProfile = "True".Equals(text4);
					break;
				case "FileName":
					processStartInfo.FileName = text4;
					break;
				case "ErrorDialog":
					processStartInfo.ErrorDialog = "True".Equals(text4);
					break;
				case "WorkingDirectory":
					processStartInfo.WorkingDirectory = text4;
					break;
				}
			}
		}
		return processStartInfo;
	}

	public bool SetBIOSOption(BIOSOption option, int state, ref long errorcode)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		long num = 0L;
		long output = 0L;
		num = 3 | (option << 8) | (state << 24);
		if (!WMISetFunction(num, ref output))
		{
			errorcode = 16777215L;
			return false;
		}
		if ((output & 0xFFFFFF) > 0)
		{
			errorcode = output & 0xFFFFFF;
			return false;
		}
		return true;
	}

	public bool WMISetFunction(long input, ref long output)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		try
		{
			ManagementObject instance = GetInstance("APGeAction");
			InvokeMethodOptions val = new InvokeMethodOptions();
			ManagementBaseObject val2 = null;
			ManagementBaseObject val3 = null;
			val2 = instance.GetMethodParameters("SetFunction");
			val2.SetPropertyValue("uiInput", (object)input);
			val3 = instance.InvokeMethod("SetFunction", val2, val);
			output = Convert.ToInt64(val3["uiOutput"]);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool SetBIOSOption_AcerGenericMethod(BIOSOption_AcerGenericMethod option, int state, ref long errorcode)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		long num = 0L;
		long output = 0L;
		num = 1 | (option << 8) | (state << 24);
		if (!WMISetFunction_AcerGenericMethod(num, ref output))
		{
			errorcode = 16777215L;
			return false;
		}
		if ((output & 0xFFFFFF) > 0)
		{
			errorcode = output & 0xFFFFFF;
			return false;
		}
		return true;
	}

	public bool WMISetFunction_AcerGenericMethod(long input, ref long output)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		try
		{
			ManagementObject instance = GetInstance("AcerGenericMethod");
			InvokeMethodOptions val = new InvokeMethodOptions();
			ManagementBaseObject val2 = null;
			ManagementBaseObject val3 = null;
			val2 = instance.GetMethodParameters("SetFunction");
			val2.SetPropertyValue("uiInput", (object)input);
			val3 = instance.InvokeMethod("SetFunction", val2, val);
			output = Convert.ToInt64(val3["uiOutput"]);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool WMIGetFunction(int input, ref long output)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		try
		{
			ManagementObject instance = GetInstance("APGeAction");
			InvokeMethodOptions val = new InvokeMethodOptions();
			ManagementBaseObject val2 = null;
			ManagementBaseObject val3 = null;
			val2 = instance.GetMethodParameters("GetFunction");
			val2.SetPropertyValue("uiInput", (object)input);
			val3 = instance.InvokeMethod("GetFunction", val2, val);
			output = Convert.ToInt64(val3["uiOutput"]);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool WMIGetFunction_BIOS(int input, ref long _errorcode, ref ushort[] modelName, ref ushort[] SN, ref int size, ref int model_len, ref int SN_len)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		try
		{
			ManagementObject instance = GetInstance("BIOSSetting");
			InvokeMethodOptions val = new InvokeMethodOptions();
			ManagementBaseObject val2 = null;
			ManagementBaseObject val3 = null;
			val2 = instance.GetMethodParameters("GetHardDrivesInformation");
			val2.SetPropertyValue("StorageID", (object)input);
			val3 = instance.InvokeMethod("GetHardDrivesInformation", val2, val);
			_errorcode = Convert.ToInt64(val3["ReturnCode"]);
			modelName = val3["ModelName"] as ushort[];
			SN = val3["SerialNumber"] as ushort[];
			size = Convert.ToInt32(val3["StorageSizeinGB"]);
			model_len = Convert.ToInt32(val3["StringLengthforModelName"]);
			SN_len = Convert.ToInt32(val3["StringLengthforSerialNumber"]);
			return true;
		}
		catch (Exception ex)
		{
			string text = ex.ToString();
			text = text;
			return false;
		}
	}

	public bool WMIGetFunction_BIOS_AcerGenericMethod(int input, ref long _errorcode, ref ushort[] modelName, ref ushort[] SN, ref int size, ref int model_len, ref int SN_len)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected Obj, but got Unknown
		try
		{
			ManagementObject instance = GetInstance("AcerGenericMethod");
			InvokeMethodOptions val = new InvokeMethodOptions();
			ManagementBaseObject val2 = null;
			ManagementBaseObject val3 = null;
			val2 = instance.GetMethodParameters("GetStorageInformation");
			val2.SetPropertyValue("StorageID", (object)input);
			val2.SetPropertyValue("Reserved", (object)input);
			val3 = instance.InvokeMethod("GetStorageInformation", val2, val);
			_errorcode = Convert.ToInt64(val3["ReturnCode"]);
			modelName = val3["ModelName"] as ushort[];
			SN = val3["SerialNumber"] as ushort[];
			size = Convert.ToInt32(val3["StorageSizeinGB"]);
			model_len = Convert.ToInt32(val3["StringLengthforModelName"]);
			SN_len = Convert.ToInt32(val3["StringLengthforSerialNumber"]);
			return true;
		}
		catch (Exception ex)
		{
			string text = ex.ToString();
			text = text;
			return false;
		}
	}

	private ManagementObject GetInstance(string ClassName)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected Obj, but got Unknown
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected Obj, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected Obj, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected Obj, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected Obj, but got Unknown
		ManagementObject result = new ManagementObject();
		ConnectionOptions val = new ConnectionOptions();
		ManagementScope val2 = new ManagementScope("\\root\\wmi", val);
		val.Impersonation = (ImpersonationLevel)3;
		val.EnablePrivileges = true;
		val2.Connect();
		WqlObjectQuery val3 = new WqlObjectQuery("select * from " + ClassName);
		ManagementObjectSearcher val4 = new ManagementObjectSearcher(val2, (ObjectQuery)(object)val3);
		ManagementObjectEnumerator enumerator = val4.Get().GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				ManagementObject val5 = (ManagementObject)enumerator.Current;
				if (val5 != null && ((ManagementBaseObject)val5)["InstanceName"] != null)
				{
					((ManagementBaseObject)val5)["InstanceName"].ToString();
					result = val5;
					break;
				}
			}
		}
		finally
		{
			((IDisposable)enumerator)?.Dispose();
		}
		((Component)(object)val4).Dispose();
		return result;
	}

	private void checkBIOSWakeOnLan()
	{
		int num = 0;
		long num2 = 0L;
		try
		{
			bool bIOSOption = wmimanagementwmi.GetBIOSOption((BIOSOption)6, ref num, ref num2);
			writeIni.WriteIni_WMILog(PLUGINSUPPORT, "WOL_isSuccess", bIOSOption.ToString(), false);
			writeIni.WriteIni_WMILog(PLUGINSUPPORT, "WOL_Status", num.ToString(), false);
			writeIni.WriteIni_WMILog(PLUGINSUPPORT, "WOL_Output", num2.ToString(), false);
		}
		catch (Exception)
		{
		}
	}

	private void checkBIOS()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		long num2 = -1L;
		bool flag = false;
		double num3 = 0.0;
		wmimanagementwmi.WMIGetFunction_BIOSVersion(ref num3);
		flag = (((!(((object)HardwareDeviceManager.GetMachineType()).ToString() == "Notebook") || !(num3 >= 2.81)) && (!(((object)HardwareDeviceManager.GetMachineType()).ToString() == "Tablet") || !(num3 >= 2.81))) ? wmimanagementwmi.GetBIOSOption((BIOSOption)16, ref num, ref num2) : wmimanagementwmi.GetBIOSOption_AcerGenericMethod((BIOSOption_AcerGenericMethod)1, ref num, ref num2));
		writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_isSuccess", flag.ToString(), false);
		writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_State", num.ToString(), false);
		writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_ErrorCode", num2.ToString(), false);
		string text = "Software\\OEM\\CareCenter\\HWMonitoring\\HWMonitor_Smart";
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text, writable: true);
		if (registryKey == null)
		{
			Registry.CurrentUser.CreateSubKey(text);
		}
		RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey(text, writable: true);
		try
		{
			registryKey2.SetValue("HWMEnable", num, RegistryValueKind.DWord);
		}
		catch (Exception)
		{
		}
	}

	private void IsSupportGetHardDriveInfo()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		int num2 = 1;
		num = num2;
		long _errorcode = -1L;
		ushort[] modelName = null;
		ushort[] SN = null;
		int size = 0;
		int model_len = 0;
		int SN_len = 0;
		try
		{
			double num3 = 0.0;
			if (num3 == 0.0)
			{
				wmimanagementwmi.WMIGetFunction_BIOSVersion(ref num3);
			}
			if (!(((!(((object)HardwareDeviceManager.GetMachineType()).ToString() == "Notebook") || !(num3 >= 2.85)) && (!(((object)HardwareDeviceManager.GetMachineType()).ToString() == "Tablet") || !(num3 >= 2.85))) ? WMIGetFunction_BIOS(num, ref _errorcode, ref modelName, ref SN, ref size, ref model_len, ref SN_len) : WMIGetFunction_BIOS_AcerGenericMethod(num, ref _errorcode, ref modelName, ref SN, ref size, ref model_len, ref SN_len)))
			{
				Log.i("[IsSupportGetHardDriveInfo] WMIGetFunction_BIOS() return code : " + _errorcode);
				Log.i("[IsSupportGetHardDriveInfo] WMIGetFunction_BIOS() return : false");
				writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_IsSupportGetHardDriveInfo", false.ToString().ToString(), false);
			}
			else if (((object)HardwareDeviceManager.GetMachineType()).ToString() == "Notebook" || ((object)HardwareDeviceManager.GetMachineType()).ToString() == "Tablet")
			{
				Log.i("[IsSupportGetHardDriveInfo] WMIGetFunction_BIOS_AcerGenericMethod() return : true");
				writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_IsSupportGetHardDriveInfo", true.ToString(), false);
			}
			else
			{
				Log.i("[IsSupportGetHardDriveInfo] WMIGetFunction_BIOS() return : true");
				writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_IsSupportGetHardDriveInfo", true.ToString(), false);
			}
		}
		catch (Exception ex)
		{
			Log.i("[IsSupportGetHardDriveInfo] exception:" + ex.ToString());
			writeIni.WriteIni_WMILog(PLUGINSUPPORT, "HWM_IsSupportGetHardDriveInfo", false.ToString().ToString(), false);
		}
	}

	public void updateAdapters()
	{
		List<Adapter> list = new List<Adapter>();
		list = queryAdaptersFromWin32();
		checkAdaptersPowerManagmentStatus(ref list);
		checkAdaptersWakeStatus(ref list);
		if (!showAllAdapter)
		{
			filterAdapters_removeIfNotSupport(ref list);
		}
		adapters = list;
	}

	public List<Adapter> queryAdaptersFromWin32()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected Obj, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected Obj, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected Obj, but got Unknown
		List<Adapter> list = new List<Adapter>();
		try
		{
			ManagementObjectSearcher val = null;
			ManagementObjectCollection val2 = null;
			Win32API_Network val3 = new Win32API_Network();
			val = new ManagementObjectSearcher("SELECT * FROM win32_networkadapter");
			val2 = val.Get();
			ManagementObjectEnumerator enumerator = val2.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					ManagementObject val4 = (ManagementObject)enumerator.Current;
					string text = string.Empty;
					string driverKeyInRegistry = string.Empty;
					string text2 = string.Empty;
					string text3 = string.Empty;
					string manufacturer = string.Empty;
					string description = string.Empty;
					string empty = string.Empty;
					string pnpDeviceID = string.Empty;
					try
					{
						text = ((ManagementBaseObject)val4).GetPropertyValue("Name").ToString();
					}
					catch
					{
					}
					try
					{
						driverKeyInRegistry = ((ManagementBaseObject)val4).GetPropertyValue("DeviceID").ToString().PadLeft(4, '0');
					}
					catch
					{
					}
					try
					{
						text2 = ((ManagementBaseObject)val4).GetPropertyValue("MACAddress").ToString();
						try
						{
							if (string.IsNullOrEmpty(text2))
							{
								text2 = val3.FindMac(text3, text);
							}
						}
						catch (Exception)
						{
							text2 = "";
						}
					}
					catch
					{
						if (string.IsNullOrEmpty(text2))
						{
							text2 = val3.FindMac(text3, text);
						}
					}
					try
					{
						text3 = ((ManagementBaseObject)val4).GetPropertyValue("GUID").ToString();
					}
					catch
					{
					}
					try
					{
						manufacturer = ((ManagementBaseObject)val4).GetPropertyValue("Manufacturer").ToString();
					}
					catch
					{
					}
					try
					{
						description = ((ManagementBaseObject)val4).GetPropertyValue("Description").ToString();
					}
					catch
					{
					}
					try
					{
						pnpDeviceID = ((ManagementBaseObject)val4).GetPropertyValue("PNPDeviceID").ToString();
					}
					catch (Exception)
					{
					}
					if (string.IsNullOrEmpty(text) || text.ToLower().Contains("virtual adapter") || text.ToLower().Contains("virtual") || text.ToLower().Equals("microsoft hosted network virtual adapter") || text.ToLower().Equals("microsoft virtual wifi miniport adapter") || string.IsNullOrEmpty(text2) || text.ToLower().IndexOf("bluetooth") != -1)
					{
						continue;
					}
					empty = val3.FindType(text3, text);
					if (!string.IsNullOrEmpty(empty) && (!empty.Equals("WLAN") || !text.ToLower().Contains("microsoft")))
					{
						if (empty.Equals("LAN"))
						{
							list.Add(new Adapter(text, driverKeyInRegistry, text2, text3, manufacturer, description, empty, pnpDeviceID));
						}
						if (showAllAdapter)
						{
							list.Add(new Adapter(text, driverKeyInRegistry, text2, text3, manufacturer, description, empty, pnpDeviceID));
						}
					}
				}
			}
			finally
			{
				((IDisposable)enumerator)?.Dispose();
			}
		}
		catch (Exception)
		{
		}
		return list;
	}

	private void checkAdaptersPowerManagmentStatus(ref List<Adapter> _adapters)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected Obj, but got Unknown
		if (_adapters == null || _adapters.Count <= 0)
		{
			return;
		}
		queryPowerManagementCollection = wmimanagementwmi.GetPowerManagementCollection();
		try
		{
			foreach (Adapter _adapter in _adapters)
			{
				ManagementObjectEnumerator enumerator2 = queryPowerManagementCollection.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						ManagementObject val = (ManagementObject)enumerator2.Current;
						string text = ((ManagementBaseObject)val).Properties["InstanceName"].Value as string;
						if (!(text == "") && !(text == string.Empty) && text.ToLower().Contains(_adapter.pnpDeviceID.ToLower()))
						{
							_adapter.setPowerManagementObj(val);
							break;
						}
					}
				}
				finally
				{
					((IDisposable)enumerator2)?.Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			Log.e("[ControlPnael] checkAdaptersPowerManagmentStatus exception: " + ex.ToString());
		}
	}

	private void checkAdaptersWakeStatus(ref List<Adapter> _adapters)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected Obj, but got Unknown
		if (_adapters == null || _adapters.Count <= 0)
		{
			return;
		}
		queryWakeCollection = wmimanagementwmi.GetWakeManagementCollection();
		try
		{
			foreach (Adapter _adapter in _adapters)
			{
				ManagementObjectEnumerator enumerator2 = queryWakeCollection.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						ManagementObject val = (ManagementObject)enumerator2.Current;
						string text = ((ManagementBaseObject)val).Properties["InstanceName"].Value as string;
						if (!(text == "") && !(text == string.Empty) && text.ToLower().Contains(_adapter.pnpDeviceID.ToLower()))
						{
							_adapter.setWakeManagementObj(val);
							break;
						}
					}
				}
				finally
				{
					((IDisposable)enumerator2)?.Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			Log.e("[ControlPnael] checkAdaptersWakeStatus exception: " + ex.ToString());
		}
	}

	private void filterAdapters_removeIfNotSupport(ref List<Adapter> _adapters)
	{
		if (_adapters.Count <= 0)
		{
			return;
		}
		List<Adapter> list = new List<Adapter>();
		foreach (Adapter _adapter in _adapters)
		{
			if (!_adapter.pnpDeviceID.Contains("USB") && _adapter.powerManagementActive && _adapter.wakeActive)
			{
				string name = Path.Combine("SYSTEM\\CurrentControlSet\\Control\\Class\\{4D36E972-E325-11CE-BFC1-08002bE10318}", _adapter.driverKeyInRegistry);
				RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
				object value = registryKey.GetValue("EnablePME");
				if (value != null)
				{
					_adapter.isSpecialCase = true;
				}
				else
				{
					Log.i("[ControlPnael] filterAdapters_removeIfNotSupport driver.name=" + _adapter.name + " no EnablePME exist");
				}
				list.Add(_adapter);
				Log.i("[ControlPnael] filterAdapters_removeIfNotSupport add_1 driver.name=" + _adapter.name + " driver.isSpecialCase" + _adapter.isSpecialCase);
			}
		}
		_adapters = list;
	}

	private void CalJunkFileSize()
	{
		long _longSizeRecycle = CleanJunkFileManager_ACCStd.Instance.GetRecycleBinSize();
		long _longSizeTemp = CleanJunkFileManager_ACCStd.Instance.GetTempFileSize();
		long _longSizeBrowser = CleanJunkFileManager_ACCStd.Instance.GetBrowserCacheSize();
		((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
		{
			writeIni.WriteIni_WMILog_JunkFile(JunkFileSizeSection, "RecycleBin", _longSizeRecycle.ToString());
			writeIni.WriteIni_WMILog_JunkFile(JunkFileSizeSection, "Temp", _longSizeTemp.ToString());
			writeIni.WriteIni_WMILog_JunkFile(JunkFileSizeSection, "Browser", _longSizeBrowser.ToString());
		}));
	}

	private void Clean(bool _chkRecycle, bool _chkTempFiles, bool _chkBrowerCache)
	{
		if (_chkRecycle)
		{
			CleanJunkFileManager_ACCStd.Instance.CleanRecycleBin();
		}
		if (_chkTempFiles)
		{
			CleanJunkFileManager_ACCStd.Instance.CleanTempFolder();
		}
		if (_chkBrowerCache)
		{
			CleanJunkFileManager_ACCStd.Instance.ClearBrowserCache();
		}
		CalJunkFileSize();
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, ShowCommands nShowCmd);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern uint GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string lpszLongPath, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszShortPath, uint cchBuffer);

	[DllImport("user32")]
	private static extern bool SetForegroundWindow(IntPtr hwnd);

	public void OpenSpecifiedFolder(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return;
		}
		try
		{
			string environmentVariable = Environment.GetEnvironmentVariable("WINDIR");
			string lpFile = environmentVariable + "\\explorer.exe";
			uint num = 0u;
			num = GetShortPathName(filePath, null, 0u);
			if (num == 0)
			{
				string lpParameters = "/select, \"" + filePath + "\"";
				ShellExecuteW(IntPtr.Zero, "", lpFile, lpParameters, "", ShowCommands.SW_SHOW);
				return;
			}
			uint num2 = num;
			StringBuilder stringBuilder = new StringBuilder(Convert.ToInt32(num2));
			uint shortPathName = GetShortPathName(filePath, stringBuilder, num2);
			string lpParameters2 = "/select, \"" + stringBuilder.ToString() + "\"";
			if (shortPathName != 0)
			{
				ShellExecuteW(IntPtr.Zero, "", lpFile, lpParameters2, "", ShowCommands.SW_SHOW);
			}
		}
		catch (Exception ex)
		{
			Log.e("Exception on open specified folder.", ex);
		}
	}

	private string GetXMLStartupInfo()
	{
		g_strTuneUpfolder = TuneupUtility.Instance.GetTuneUpCommonWorkFolder();
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wdi\\LogFiles\\StartupInfo");
		string text = Path.Combine(g_strTuneUpfolder, "Trace");
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		string sIDstring = TuneupUtility.Instance.GetSIDstring();
		string text2 = Path.Combine(path, sIDstring.ToString() + "_StartupInfo1.xml");
		string text3 = text2;
		string empty = string.Empty;
		int num = 1;
		for (int i = 1; i < 5; i++)
		{
			text3 = text3.Replace(i + ".xml", i + 1 + ".xml");
			if (File.Exists(text3))
			{
				if (!File.Exists(text2))
				{
					text2 = text3;
					num = i + 1;
				}
				else if (File.GetLastWriteTime(text2) < File.GetLastWriteTime(text3))
				{
					text2 = text3;
					num = i + 1;
				}
			}
		}
		if (!File.Exists(text2))
		{
			Log.e("[ParseXMLStartupInfo] No boot xml file.");
			return string.Empty;
		}
		empty = Path.Combine(text, sIDstring.ToString() + "_StartupInfo" + num + ".xml");
		try
		{
			if (!File.Exists(empty) || File.GetLastWriteTime(text2) > File.GetLastWriteTime(empty))
			{
				StartupInfoXMLParser.Instance.Parser_DivideAndReplace(text2, empty);
			}
		}
		catch (Exception ex)
		{
			Log.e("[ParseXMLStartupInfo] Boot xml file is used.", ex);
			return string.Empty;
		}
		return empty;
	}

	private void Launch_SoftwareAssembler_ACCUI()
	{
		try
		{
			string text = CheckEnvironment.Instance.GetInstallPath() + "\\ACCSWALauncher.exe";
			if (File.Exists(text))
			{
				Log.i("[APP_Launch] SoftwareAssembler_Launch exist is :" + text);
				NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "treadstone_service_LightMode", PipeDirection.InOut);
				namedPipeClientStream.Connect();
				IPCMethods.SendCommandByNamedPipe(namedPipeClientStream, 7, new object[1] { 102502 });
				namedPipeClientStream.WaitForPipeDrain();
				namedPipeClientStream.Close();
				Log.i("[APP_Launch] SoftwareAssembler_Launch be lauched succeddful!");
			}
		}
		catch (Exception)
		{
		}
	}

	private void UWPLauncher(string str_PluginType, string str_PluginParameter = "")
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		string empty = string.Empty;
		ModelType val = CheckEnvironment.Instance.CheckModelType();
		empty = (((int)val != 0) ? CheckEnvironment.appIDorPath_ACtC : CheckEnvironment.appIDorPath_ACC);
		string arguments = str_PluginType + " " + str_PluginParameter;
		try
		{
			ApplicationActivationManager applicationActivationManager = new ApplicationActivationManager();
			applicationActivationManager.ActivateApplication(empty, arguments, ActivateOptions.None, out var _);
		}
		catch (Exception)
		{
		}
	}

	private string GetAuthKey_ACCStd()
	{
		string text = string.Empty;
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(CommDef._RegPath_Agent);
			if (registryKey != null)
			{
				try
				{
					object value = registryKey.GetValue(CommDef._RegKey_ConsoleName);
					if (value != null)
					{
						Log.i("[ACCStd][GetAuthKey_ACCStd] regkey.GetValue");
						text = "1";
					}
				}
				catch
				{
					text = string.Empty;
				}
			}
		}
		catch (Exception ex)
		{
			Log.i("[ACCStd][GetAuthKey_ACCStd] Exception : " + ex.ToString());
			text = string.Empty;
		}
		if (text != string.Empty)
		{
			text = "1";
		}
		writeIni.WriteIni_SP("AOM", "IsManaged", text, (IniLocation)1);
		return text;
	}

	private void Launch_FixpackFolderCleaner()
	{
		try
		{
			string text = CheckEnvironment.Instance.GetInstallPath() + "\\ACCFixpackFolderCleaner.exe";
			if (File.Exists(text))
			{
				Log.i("[APP_Launch] APP_Launch exist is :" + text);
				NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "treadstone_service_LightMode", PipeDirection.InOut);
				namedPipeClientStream.Connect();
				IPCMethods.SendCommandByNamedPipe(namedPipeClientStream, 7, new object[1] { 102503 });
				namedPipeClientStream.WaitForPipeDrain();
				namedPipeClientStream.Close();
				Log.i("[APP_Launch] APP_Launch be lauched succeddful!");
			}
		}
		catch (Exception)
		{
		}
	}

	private void PeriodCheckSelfupgrade_funcion()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Invalid comparison between Unknown and I4
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		if (NetworkInterface.GetIsNetworkAvailable())
		{
			Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] Network is Available");
			if ((int)CheckEnvironment.Instance.CheckModelType() == 1 && Process.GetProcessesByName(ACtC_UWP_Name).Length > 0)
			{
				Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] UWP launched, change period time to  1 hr");
				_timeForSelfupgradeCheck.Change(3600 * _delayTime, 3600 * _delayTime);
				return;
			}
			if ((int)CheckEnvironment.Instance.CheckModelType() == 0 && Process.GetProcessesByName(ACC_UWP_Name).Length > 0)
			{
				Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] UWP launched, change period time to 1 hr");
				_timeForSelfupgradeCheck.Change(3600 * _delayTime, 3600 * _delayTime);
				return;
			}
			Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] No UWP launched, start to launch upgrade tool, change period time to 7 days");
			_timeForSelfupgradeCheck.Change(10080 * _delayTime, 10080 * _delayTime);
			Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] Function start on : " + DateTime.Now.ToString("yyyy/MM/dd"));
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OEM\\UpgradeTool\\CareCenter_v4\\UpgradeTool.exe");
			if ((int)CheckEnvironment.Instance.CheckModelType() == 1)
			{
				text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OEM\\UpgradeTool\\ControlCenter_v4\\UpgradeTool.exe");
			}
			else if ((int)CheckEnvironment.Instance.CheckModelType() == 0)
			{
				text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OEM\\UpgradeTool\\CareCenter_v4\\UpgradeTool.exe");
			}
			if (File.Exists(text))
			{
				Process process = new Process();
				process.StartInfo.FileName = text;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.Arguments = "-supdate";
				process.Start();
				process.WaitForExit();
				Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] ExitCode : " + process.ExitCode);
				if (process.ExitCode != 1124 && process.ExitCode != 1125)
				{
					UpdaterEnvironment.LastSelfUpdateTime = DateTime.Now;
				}
			}
		}
		else
		{
			Log.i("[ACCStd] [PeriodCheckSelfupgrade_funcion] Network is not available, change period time to 1 hr");
			_timeForSelfupgradeCheck.Change(3600 * _delayTime, 3600 * _delayTime);
		}
	}

	private void PeriodCheckSelfupgrade(object state)
	{
		Thread thread = new Thread(PeriodCheckSelfupgrade_funcion);
		thread.Start();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[DebuggerNonUserCode]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected Obj, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected Obj, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected Obj, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected Obj, but got Unknown
		switch (connectionId)
		{
		case 1:
			((FrameworkElement)(MainWindow)target).Loaded += Window_Loaded;
			((Window)(MainWindow)target).Closing += Window_Closing;
			break;
		case 2:
			((ButtonBase)(Button)target).Click += Start_Click;
			break;
		case 3:
			((ButtonBase)(Button)target).Click += Stop_Click;
			break;
		case 4:
			((ButtonBase)(Button)target).Click += Update_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
