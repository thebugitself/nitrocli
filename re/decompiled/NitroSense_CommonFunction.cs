using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TsDotNetLib;

namespace NitroSense;

public class CommonFunction
{
	public struct SYSTEM_POWER_STATUS
	{
		public byte ACLineStatus;

		public byte BatteryFlag;

		public byte BatteryLifePercent;

		public byte Reserved1;

		public int BatteryLifeTime;

		public int BatteryFullLifeTime;
	}

	public struct SKEY
	{
		public uint cbSize;

		public uint dwFlags;
	}

	public class Promotion_App_info
	{
		public string App_name;

		public List<string> App_description;

		public DateTime App_date;

		public string folder_path;

		public string App_path;
	}

	public enum System_Health_Information_Index
	{
		sCPU_Temperature = 1,
		sCPU_Fan_Speed,
		sSystem_Temperature,
		sSystem_Fan_Speed,
		sFrostCore,
		sGPU_Fan_Speed,
		sSystem2_Temperature,
		sSystem2_Fan_Speed,
		sGPU2_Fan_Speed,
		sGPU1_Temperature,
		sGPU2_Temperature
	}

	public enum Color_Button_Type
	{
		cHotkey_Group1,
		cHotkey_Group2,
		cHotkey_Group3
	}

	public enum Export_Profile_Type
	{
		eSingle,
		aMultiple
	}

	public enum Attention_Type
	{
		aDelete_Profile,
		aEnough_Storage,
		aImport_Failed,
		aMacro_Record_Max,
		aProfile_Same_Name,
		aMacro_Same_Name,
		aGroup_Same_Name,
		aDelete_Macro
	}

	public enum Profile_Type
	{
		pHotkey,
		pLighting,
		pMacro,
		pRename_Macro
	}

	public enum SoundMode_Type
	{
		Music,
		Movies,
		General,
		Voice,
		FPS,
		Sport
	}

	public enum DTS_SoundMode_Type
	{
		Music,
		Movie,
		Voice,
		Strategy,
		RPG,
		Shooter,
		Custom_Audio,
		Off,
		Unlicensed,
		NumOperatingModes,
		Automatic
	}

	public enum PowerPlanMode_Type
	{
		PowerSaver,
		Balance,
		BalanceAcer,
		HighPerformance
	}

	public enum OPMode_Type
	{
		Quiet,
		Default,
		Render,
		Edit,
		Extreme,
		Turbo
	}

	public enum AC_Type
	{
		DC,
		AC
	}

	public enum Key_Assignment_Type
	{
		kCPU_Overclocking_Switch,
		kGPU_Overclocking_Switch,
		kFan_Speed_Switch,
		kLaunchApp,
		kAdvanced_Setting_Switch,
		kMacro,
		kNone
	}

	public enum CPU_Assignment_Option
	{
		oNormal_Faster_Turbo,
		oNormal_Faster,
		oFaster_Turbo,
		oNormal_Turbo
	}

	public enum GPU_Assignment_Option
	{
		oNormal_Faster_Turbo,
		oNormal_Faster,
		oFaster_Turbo,
		oNormal_Turbo
	}

	public enum Fan_Assignment_Option
	{
		oAuto_Max_Custom,
		oAuto_Max,
		oMax_Custom,
		oAuto_Custom
	}

	public enum Setting_Assignment_Option
	{
		oSticky_Keys,
		oWindows_menu_key,
		oLCD_Overdrive
	}

	public enum Degree_Type
	{
		dCelsius,
		dFahrenheit
	}

	public enum Fan_Mode_Type
	{
		Auto,
		Max,
		Custom
	}

	public enum Fan_Group_Type
	{
		fCPU,
		fGPU
	}

	public enum Backlight_Status
	{
		tab_static,
		tab_dynamic,
		tab_off
	}

	public enum Zone_Color_Type
	{
		Zone_1 = 1,
		Zone_2,
		Zone_3,
		Zone_4
	}

	public enum Overclock_Mode_Type
	{
		Normal,
		Faster,
		Turbo
	}

	public enum AC_Mode_Type
	{
		aNo_AC,
		a1_AC,
		a2_AC
	}

	public enum GPU_Info_Type
	{
		gGet_GPU_Frequency = 1,
		gGet_GPU_Max_Frequency
	}

	public enum CPU_Info_Type
	{
		cIsSupportOC = 1,
		cGet_CPU_Frequency,
		cGet_CPU_Max_Frequency
	}

	public enum Frequency_Mode
	{
		fNormal,
		fMax
	}

	public enum MachineType
	{
		Freed,
		Octavia,
		Jimny
	}

	public static double[] zone_kb_adjust = new double[3] { 0.8, 1.0, 1.0 };

	public static string session_id = Process.GetCurrentProcess().SessionId.ToString();

	public static string fahrenheit_name = Startup.langRd[(object)"MUI_Fahrenheit"].ToString();

	public static string celsius_name = Startup.langRd[(object)"MUI_Celsius"].ToString();

	public static string select_macro_name = "--" + Startup.langRd[(object)"MUI_Select_Macro"].ToString() + "--";

	public static string macro_content_name = Startup.langRd[(object)"MUI_Macro"].ToString();

	public static string profile_content_name = Startup.langRd[(object)"MUI_Profile"].ToString();

	public static string default_content_name = "Default";

	public static string hotkey_profile_path = "C:\\ProgramData\\oem\\NitroSense\\ProfilePool\\HotkeyProfilePool\\";

	public static string lighting_profile_path = "C:\\ProgramData\\oem\\NitroSense\\ProfilePool\\LightProfilePool\\";

	public static string macro_profile_path = "C:\\ProgramData\\oem\\NitroSense\\ProfilePool\\HotkeyProfilePool\\%HotkeyProfileName%\\MacroKeyPool\\";

	public static string _temp_dir_path = "C:\\ProgramData\\oem\\NitroSense\\ProfilePool\\exception_temp";

	public static string macro_profile_path_replace_name = "%HotkeyProfileName%";

	public static int SoundMode = -1;

	public static char[] all_whitespaces = new char[28]
	{
		' ', '\u1680', '\u180e', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006',
		'\u2007', '\u2008', '\u2009', '\u200a', '\u202f', '\u205f', '\u3000', '\u2028', '\u2029', '\t',
		'\n', '\v', '\f', '\r', '\u0085', '\u00a0', '\u200b', '\ufeff'
	};

	public static double adjust_red = 0.5;

	public static string NitroSense_HW_support_path_ini = "C:\\ProgramData\\oem\\NitroSense\\HW_Support.ini";

	public static bool complete_loading = false;

	public static int gpu_count = 0;

	public static List<Promotion_App_info> Promotion_App_List = new List<Promotion_App_info>();

	public static List<Promotion_App_info> Promotion_App_List_NewFeature = new List<Promotion_App_info>();

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

	[DllImport("user32.dll")]
	private static extern bool SystemParametersInfo(uint action, uint param, ref SKEY vparam, uint init);

	[DllImport("kernel32")]
	private static extern void GetSystemPowerStatus(ref SYSTEM_POWER_STATUS lpSystemPowerStatus);

	public static void exception_move_folder(string source, string dest, string new_name)
	{
		if (Directory.Exists(_temp_dir_path))
		{
			Directory.Delete(_temp_dir_path, recursive: true);
		}
		Directory.CreateDirectory(_temp_dir_path + "\\" + new_name);
		DirectoryCopy(source, _temp_dir_path + "\\" + new_name, copySubDirs: true);
		_ = delete_file(source).Result;
		Directory.Move(_temp_dir_path + "\\" + new_name, dest);
		Directory.Delete(_temp_dir_path, recursive: true);
	}

	public static void exception_move_file(string source, string dest, string new_name)
	{
		if (Directory.Exists(_temp_dir_path))
		{
			Directory.Delete(_temp_dir_path, recursive: true);
		}
		Directory.CreateDirectory(_temp_dir_path);
		File.Copy(source, _temp_dir_path + "\\" + new_name, overwrite: true);
		_ = delete_file(source).Result;
		Directory.Move(_temp_dir_path + "\\" + new_name, dest);
		Directory.Delete(_temp_dir_path, recursive: true);
	}

	private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirName);
		if (!directoryInfo.Exists)
		{
			return;
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			string destFileName = Path.Combine(destDirName, fileInfo.Name);
			fileInfo.CopyTo(destFileName, overwrite: false);
		}
		if (copySubDirs)
		{
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo2 in array)
			{
				string destDirName2 = Path.Combine(destDirName, directoryInfo2.Name);
				DirectoryCopy(directoryInfo2.FullName, destDirName2, copySubDirs);
			}
		}
	}

	public static bool UpdateImage(Image sender, string name)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected Obj, but got Unknown
		try
		{
			Uri uri = new Uri(Startup._Image_path + name, UriKind.Absolute);
			sender.Source = (ImageSource)new BitmapImage(uri);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static int get_battery_life_percent()
	{
		SYSTEM_POWER_STATUS lpSystemPowerStatus = default;
		GetSystemPowerStatus(ref lpSystemPowerStatus);
		return lpSystemPowerStatus.BatteryLifePercent;
	}

	public static async Task<int> delete_file(string file_path)
	{
		int output = 0;
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_admin_agent_" + session_id, PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 3, file_path);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static async Task<int> delete_folder(string file_path)
	{
		int output = 0;
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_admin_agent_" + session_id, PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 4, file_path);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static Color ColorFromString(string htmlColor)
	{
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			htmlColor = htmlColor.Replace("#", "");
			byte b = byte.MaxValue;
			byte b2 = 0;
			byte b3 = 0;
			byte b4 = 0;
			switch (htmlColor.Length)
			{
			case 3:
				b2 = byte.Parse(htmlColor.Substring(0, 1), NumberStyles.HexNumber);
				b3 = byte.Parse(htmlColor.Substring(1, 1), NumberStyles.HexNumber);
				b4 = byte.Parse(htmlColor.Substring(2, 1), NumberStyles.HexNumber);
				break;
			case 4:
				b = byte.Parse(htmlColor.Substring(0, 1), NumberStyles.HexNumber);
				b2 = byte.Parse(htmlColor.Substring(1, 1), NumberStyles.HexNumber);
				b3 = byte.Parse(htmlColor.Substring(2, 1), NumberStyles.HexNumber);
				b4 = byte.Parse(htmlColor.Substring(3, 1), NumberStyles.HexNumber);
				break;
			case 6:
				b2 = byte.Parse(htmlColor.Substring(0, 2), NumberStyles.HexNumber);
				b3 = byte.Parse(htmlColor.Substring(2, 2), NumberStyles.HexNumber);
				b4 = byte.Parse(htmlColor.Substring(4, 2), NumberStyles.HexNumber);
				break;
			case 8:
				b = byte.Parse(htmlColor.Substring(0, 2), NumberStyles.HexNumber);
				b2 = byte.Parse(htmlColor.Substring(2, 2), NumberStyles.HexNumber);
				b3 = byte.Parse(htmlColor.Substring(4, 2), NumberStyles.HexNumber);
				b4 = byte.Parse(htmlColor.Substring(6, 2), NumberStyles.HexNumber);
				break;
			}
			return Color.FromArgb(b, b2, b3, b4);
		}
		catch (Exception)
		{
			return Color.FromArgb(byte.MaxValue, (byte)0, (byte)0, (byte)0);
		}
	}

	public static string StringFromColor(Color bg_color)
	{
		try
		{
			_ = string.Empty;
			return $"#{bg_color.R:X2}{bg_color.G:X2}{bg_color.B:X2}";
		}
		catch (Exception)
		{
			return "#FFFF00";
		}
	}

	public static void Get_type_and_option_name(int type_index, int option_index, ref string type_name, ref string option_name)
	{
		if (type_index == 0)
		{
			type_name = Startup.langRd[(object)"MUI_CPU_OC_Switch"].ToString();
			if (option_index == 0)
			{
				option_name = Startup.langRd[(object)"MUI_Normal"].ToString() + " > " + Startup.langRd[(object)"MUI_Faster"].ToString() + " > " + Startup.langRd[(object)"MUI_Turbo"].ToString();
			}
			else if (1 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Normal"].ToString() + " <> " + Startup.langRd[(object)"MUI_Faster"].ToString();
			}
			else if (2 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Faster"].ToString() + " <> " + Startup.langRd[(object)"MUI_Turbo"].ToString();
			}
			else if (3 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Normal"].ToString() + " <> " + Startup.langRd[(object)"MUI_Turbo"].ToString();
			}
		}
		else if (1 == type_index)
		{
			type_name = Startup.langRd[(object)"MUI_GPU_OC_Switch"].ToString();
			if (option_index == 0)
			{
				option_name = Startup.langRd[(object)"MUI_Normal"].ToString() + " > " + Startup.langRd[(object)"MUI_Faster"].ToString() + " > " + Startup.langRd[(object)"MUI_Turbo"].ToString();
			}
			else if (1 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Normal"].ToString() + " <> " + Startup.langRd[(object)"MUI_Faster"].ToString();
			}
			else if (2 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Faster"].ToString() + " <> " + Startup.langRd[(object)"MUI_Turbo"].ToString();
			}
			else if (3 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Normal"].ToString() + " <> " + Startup.langRd[(object)"MUI_Turbo"].ToString();
			}
		}
		else if (2 == type_index)
		{
			type_name = Startup.langRd[(object)"MUI_Fan_Switch"].ToString();
			if (option_index == 0)
			{
				option_name = Startup.langRd[(object)"MUI_Auto"].ToString() + " > " + Startup.langRd[(object)"MUI_Max"].ToString() + " > " + Startup.langRd[(object)"MUI_Custom"].ToString();
			}
			else if (1 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Auto"].ToString() + " <> " + Startup.langRd[(object)"MUI_Max"].ToString();
			}
			else if (2 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Max"].ToString() + " <> " + Startup.langRd[(object)"MUI_Custom"].ToString();
			}
			else if (3 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Auto"].ToString() + " <> " + Startup.langRd[(object)"MUI_Custom"].ToString();
			}
		}
		else if (3 == type_index)
		{
			type_name = Startup.langRd[(object)"MUI_Launch_App"].ToString();
		}
		else if (4 == type_index)
		{
			type_name = Startup.langRd[(object)"MUI_ED_Advance_Setting"].ToString();
			if (option_index == 0)
			{
				option_name = Startup.langRd[(object)"MUI_Sticky_Key"].ToString();
			}
			else if (1 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_Win_And_Menu_Key"].ToString();
			}
			else if (2 == option_index)
			{
				option_name = Startup.langRd[(object)"MUI_LCD_Overdrive"].ToString();
			}
		}
		else if (5 == type_index)
		{
			type_name = Startup.langRd[(object)"MUI_Macro"].ToString();
		}
		else if (6 == type_index)
		{
			type_name = "(" + Startup.langRd[(object)"MUI_None"].ToString() + ")";
		}
	}

	public static long GetDirectorySize(string dir)
	{
		try
		{
			FileInfo fileInfo = null;
			string[] files = Directory.GetFiles(dir);
			string[] directories = Directory.GetDirectories(dir);
			long num = 0L;
			string[] array = files;
			for (int i = 0; i < array.Length; i++)
			{
				fileInfo = new FileInfo(array[i]);
				num += fileInfo.Length;
			}
			array = directories;
			foreach (string dir2 in array)
			{
				num += GetDirectorySize(dir2);
			}
			return num;
		}
		catch (Exception)
		{
			return 0L;
		}
	}

	public static double Get_Free_Size(string drive)
	{
		try
		{
			double result = 0.0;
			if (!GetDiskFreeSpaceEx(drive, out var _, out var _, out var lpTotalNumberOfFreeBytes))
			{
				return result;
			}
			return (double)(long)lpTotalNumberOfFreeBytes / 1024.0 / 1024.0;
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	public static string Get_Group_Head_Name(int group_index)
	{
		string result = "";
		switch (group_index)
		{
		case 0:
			result = "r";
			break;
		case 1:
			result = "b";
			break;
		case 2:
			result = "g";
			break;
		}
		return result;
	}

	public static bool get_current_group_index(ref int group_index)
	{
		uint input = 0u;
		ulong result = Task<ulong>.Factory.StartNew(() => WMIFunction.GetAcerGamingPorfileConfiguration(input).GetAwaiter().GetResult()).Result;
		if ((result & 0xFF) == 0L)
		{
			group_index = (int)((result >> 32) & 0xFF);
			if (group_index == 0)
			{
				group_index = 1;
			}
			return true;
		}
		return false;
	}

	public static void set_current_group_index(int group_index)
	{
		WMIFunction.SetAcerGamingPorfileConfiguration((ulong)(((long)group_index << 32) | 4)).GetAwaiter();
	}

	public static void set_win_menu_key_status(bool status)
	{
		WMIFunction.SetAcerGamingPorfileConfiguration((ulong)(((long)(status ? 1 : 0) << 24) | 2)).GetAwaiter();
	}

	public static void set_lcd_over_drive(bool state)
	{
		WMIFunction.SetAcerGamingPorfileConfiguration((ulong)(0x10 | (state ? 281474976710656L : 0))).GetAwaiter();
	}

	public static bool get_lcd_over_drive()
	{
		uint input = 0u;
		ulong result = Task<ulong>.Factory.StartNew(() => WMIFunction.GetAcerGamingPorfileConfiguration(input).GetAwaiter().GetResult()).Result;
		if ((result & 0xFF) == 0L)
		{
			if (((result >> 48) & 0xFF) == 1)
			{
				return true;
			}
			return false;
		}
		return false;
	}

	public static bool get_backlight_off()
	{
		int num = Registry.CheckLM("SOFTWARE\\OEM\\NitroSense", "BK_Hotkey_Number");
		ulong result = WMIFunction.WMIGetFunction((uint)(1 | (num << 8) | 0x80000)).GetAwaiter().GetResult();
		if ((result & 0xFF) == 0L && ((result >> 40) & 0xFF) == 30)
		{
			return true;
		}
		return false;
	}

	public static ulong get_backlight_brightness_percentage()
	{
		int num = Registry.CheckLM("SOFTWARE\\OEM\\NitroSense", "BK_Hotkey_Number");
		ulong result = WMIFunction.WMIGetFunction((uint)(1 | (num << 8) | 0x80000)).GetAwaiter().GetResult();
		if ((result & 0xFF) == 0L)
		{
			return (result >> 32) & 0xFF;
		}
		return 100uL;
	}

	public static void set_group_wmi_color(int group_index, Color color)
	{
		ulong num = Convert.ToUInt64(color.R);
		num = Convert.ToUInt64(Math.Ceiling((double)num * 0.5));
		ulong num2 = Convert.ToUInt64(color.G);
		ulong num3 = Convert.ToUInt64(color.B);
		WMIFunction.SetAcerGamingLEDGroupColor((ulong)(0xFF & group_index) | ((num << 8) | (num2 << 16) | (num3 << 24))).GetAwaiter();
	}

	public static bool get_wmi_system_health_info(ref int info_data, System_Health_Information_Index index)
	{
		ulong result = WMIFunction.GetAcerGamingSystemInformation((uint)(1 | ((int)index << 8))).Result;
		if ((result & 0xFF) == 0L)
		{
			info_data = (int)((result >> 8) & 0xFFFF);
			return true;
		}
		return false;
	}

	public static bool get_wmi_battery_boost_status(ref bool status)
	{
		status = false;
		ulong result = WMIFunction.GetAcerGamingSystemInformation(2u).Result;
		if ((result & 0xFF) == 0L)
		{
			if (((result >> 40) & 0xFF) == 1)
			{
				status = true;
			}
			else
			{
				status = false;
			}
			return true;
		}
		return false;
	}

	public static async Task<List<ulong>> get_gpu_usage_loading()
	{
		List<ulong> output = new List<ulong>();
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				List<ulong> list = new List<ulong>();
				IPCMethods.SendCommandByNamedPipe(cline_stream, 14);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[25];
				cline_stream.Read(array, 0, array.Length);
				list.Add(BitConverter.ToUInt64(array, 5));
				list.Add(BitConverter.ToUInt64(array, 17));
				return list;
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static async Task<List<int>> get_gpu_frequency(Frequency_Mode frequency_mode)
	{
		List<int> output = new List<int>();
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				List<int> list = new List<int>();
				if (frequency_mode == Frequency_Mode.fMax)
				{
					IPCMethods.SendCommandByNamedPipe(cline_stream, 21, 2);
				}
				else if (frequency_mode == Frequency_Mode.fNormal)
				{
					IPCMethods.SendCommandByNamedPipe(cline_stream, 21, 1);
				}
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[17];
				cline_stream.Read(array, 0, array.Length);
				list.Add(BitConverter.ToInt32(array, 5));
				list.Add(BitConverter.ToInt32(array, 13));
				return list;
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static async Task<int> get_cpu_frequency(Frequency_Mode frequency_mode)
	{
		int output = -1;
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				if (frequency_mode == Frequency_Mode.fMax)
				{
					IPCMethods.SendCommandByNamedPipe(cline_stream, 22, 3);
				}
				else if (frequency_mode == Frequency_Mode.fNormal)
				{
					IPCMethods.SendCommandByNamedPipe(cline_stream, 22, 2);
				}
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static async Task<int> set_cpu_oc_level(Overclock_Mode_Type level)
	{
		int output = -1;
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 24, (int)level);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static async Task<int> set_gpu_oc_level(Overclock_Mode_Type level)
	{
		int output = -1;
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 23, (int)level);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static int transfer_celsius_to_fahrenheit_name(int celsius)
	{
		return (int)((float)celsius * 9f / 5f + 32f);
	}

	public static int transfer_fahrenheit_to_celsius_name(int fahrenheit)
	{
		return (int)(float)((fahrenheit - 32) * 5 / 9);
	}

	public static bool set_all_fan_mode(Fan_Mode_Type mode_index)
	{
		ulong num = 9uL;
		switch (mode_index)
		{
		case Fan_Mode_Type.Auto:
			num |= 0x410000;
			break;
		case Fan_Mode_Type.Max:
			num |= 0x820000;
			break;
		case Fan_Mode_Type.Custom:
			num |= 0xC30000;
			break;
		}
		if ((WMIFunction.SetAcerGamingFanGroupBehavior(num).GetAwaiter().GetResult() & 0xFF) == 0)
		{
			return true;
		}
		return false;
	}

	public static bool set_coolboost_state(bool state)
	{
		if ((WMIFunction.WMISetFunction(7uL | (ulong)((state ? 1 : 0) << 16)).GetAwaiter().GetResult() & 0xFF) == 0)
		{
			return true;
		}
		return false;
	}

	public static bool set_all_custom_fan_state(List<bool> auto, List<ulong> percentage)
	{
		bool result = true;
		ulong num = 0uL;
		ulong num2 = 0uL;
		ulong num3 = 0uL;
		num2 = (ulong)(auto[0] ? 1 : 3);
		num3 = (ulong)(auto[1] ? 1 : 3);
		num = 9 | (num2 << 16) | (num3 << 22);
		if ((WMIFunction.SetAcerGamingFanGroupBehavior(num).GetAwaiter().GetResult() & 0xFF) != 0)
		{
			result = false;
		}
		for (int i = 0; i < 2; i++)
		{
			switch (i)
			{
			case 0:
				num = 1uL;
				break;
			case 1:
				num = 4uL;
				break;
			}
			num |= percentage[i] << 8;
			if ((WMIFunction.SetAcerGamingFanGroupSpeed(num).GetAwaiter().GetResult() & 0xFF) != 0)
			{
				result = false;
			}
		}
		return result;
	}

	public static bool set_single_custom_fan_state(bool auto, ulong percentage, Fan_Group_Type fan_group_type)
	{
		bool result = true;
		ulong acerGamingFanGroupBehavior = 0uL;
		ulong num = 0uL;
		ulong num2 = (ulong)(auto ? 1 : 3);
		switch (fan_group_type)
		{
		case Fan_Group_Type.fCPU:
			acerGamingFanGroupBehavior = 1 | (num2 << 16);
			num = 1uL;
			break;
		case Fan_Group_Type.fGPU:
			acerGamingFanGroupBehavior = 8 | (num2 << 22);
			num = 4uL;
			break;
		}
		if ((WMIFunction.SetAcerGamingFanGroupBehavior(acerGamingFanGroupBehavior).GetAwaiter().GetResult() & 0xFF) != 0)
		{
			result = false;
		}
		num |= percentage << 8;
		if ((WMIFunction.SetAcerGamingFanGroupSpeed(num).GetAwaiter().GetResult() & 0xFF) != 0)
		{
			result = false;
		}
		return result;
	}

	public static bool set_single_custom_fan_speed(ulong percentage, Fan_Group_Type fan_group_type)
	{
		ulong num = 0uL;
		switch (fan_group_type)
		{
		case Fan_Group_Type.fCPU:
			num = 1uL;
			break;
		case Fan_Group_Type.fGPU:
			num = 4uL;
			break;
		}
		num |= percentage << 8;
		WMIFunction.SetAcerGamingFanGroupSpeed(num).GetAwaiter();
		return true;
	}

	public static bool get_coolboost_state()
	{
		ulong result = WMIFunction.WMIGetFunction(519u).GetAwaiter().GetResult();
		if ((result & 0xFF) == 0L && ((result >> 8) & 0xFF) == 1)
		{
			return true;
		}
		return false;
	}

	public static bool get_current_ac_mode(ref AC_Mode_Type current_ac_mode)
	{
		try
		{
			int num = 0;
			num = ((Registry.CheckLM("SOFTWARE\\OEM\\NitroSense", "PD_Support") == 1) ? 1 : 0);
			if (num == 1)
			{
				ulong result = WMIFunction.WMIGetFunction(10u).GetAwaiter().GetResult();
				if ((result & 0xFF) == 0L)
				{
					if ((int)((result >> 8) & 1) == 1)
					{
						current_ac_mode = AC_Mode_Type.a2_AC;
					}
					else if ((int)((result >> 8) & 4) == 4 || (int)((result >> 8) & 8) == 8)
					{
						current_ac_mode = AC_Mode_Type.aNo_AC;
					}
					else
					{
						current_ac_mode = AC_Mode_Type.aNo_AC;
					}
					return true;
				}
				return false;
			}
			SYSTEM_POWER_STATUS lpSystemPowerStatus = default;
			GetSystemPowerStatus(ref lpSystemPowerStatus);
			if (lpSystemPowerStatus.ACLineStatus == 1)
			{
				current_ac_mode = AC_Mode_Type.a2_AC;
			}
			else
			{
				current_ac_mode = AC_Mode_Type.aNo_AC;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool set_usb_kb_color(List<Color> colors)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		bool result = true;
		byte[] array = new byte[512];
		int num = 0;
		foreach (Color color in colors)
		{
			Color current = color;
			byte r = current.R;
			byte g = current.G;
			byte b = current.B;
			array[num * 4 + 1] = r;
			array[num * 4 + 2] = g;
			array[num * 4 + 3] = b;
			num++;
		}
		if ((USBFunction.SetUSBKeyboardColor(array).GetAwaiter().GetResult() & 0xFF) != 0)
		{
			result = false;
		}
		return result;
	}

	public static bool set_usb_kb_pattern(ulong iEffect, ulong iSpeed, Color cColor, ulong iDirect)
	{
		bool result = true;
		byte[] cColor2 = new byte[4] { 0, cColor.R, cColor.G, cColor.B };
		USBFunction.SetUSBPattern(Convert.ToByte(iEffect), Convert.ToByte(Math.Abs((int)iSpeed - 10)), cColor2, Convert.ToByte(iDirect)).GetAwaiter();
		if ((0 & 0xFF) != 0)
		{
			result = false;
		}
		return result;
	}

	public static bool set_usb_kb_off()
	{
		bool result = true;
		_ = new byte[512];
		byte[] cColor = new byte[4];
		if ((USBFunction.SetUSBPattern(Convert.ToByte(0), Convert.ToByte(0), cColor, Convert.ToByte(0)).GetAwaiter().GetResult() & 0xFF) != 0)
		{
			result = false;
		}
		return result;
	}

	public static bool set_usb_kb_on_color(List<Color> colors)
	{
		return false;
	}

	public static bool set_usb_kb_on_pattern(ulong iEffect, ulong iSpeed, Color cColor, ulong iDirect)
	{
		bool result = true;
		byte[] cColor2 = new byte[4] { 0, cColor.R, cColor.G, cColor.B };
		USBFunction.SetUSBPattern(Convert.ToByte(iEffect), Convert.ToByte(Math.Abs((int)iSpeed - 10)), cColor2, Convert.ToByte(iDirect)).GetAwaiter();
		if ((0 & 0xFF) != 0)
		{
			result = false;
		}
		return result;
	}

	public static int get_usb_pad_direction()
	{
		return (int)USBFunction.GetUSBPad().GetAwaiter().GetResult();
	}

	public static bool set_usb_pad_off()
	{
		USBFunction.SetUSBPad(0uL).GetAwaiter().GetResult();
		return false;
	}

	public static bool set_usb_pad_on()
	{
		USBFunction.SetUSBPad(1uL).GetAwaiter().GetResult();
		return false;
	}

	public static bool set_usb_pad_on_color(Color color)
	{
		USBFunction.SetUSBPad(2uL, new byte[4] { 0, color.R, color.G, color.B }).GetAwaiter();
		return false;
	}

	public static bool set_wmi_Logo_color(int index, Color color)
	{
		uint num = 0u;
		ulong num2 = Convert.ToUInt64(Convert.ToDouble(Convert.ToUInt64(color.R)) * adjust_red);
		ulong num3 = color.G;
		ulong num4 = color.B;
		switch (index)
		{
		case 1:
			WMIFunction.WMISetLogoLED(1 | (num2 << 8) | (num3 << 16) | (num4 << 24)).GetAwaiter();
			break;
		case 2:
			WMIFunction.WMISetLogoLED(2 | (num2 << 8) | (num3 << 16) | (num4 << 24)).GetAwaiter();
			WMIFunction.WMISetLogoLED(3 | (num2 << 8) | (num3 << 16) | (num4 << 24)).GetAwaiter();
			break;
		case 3:
			WMIFunction.WMISetLogoLED(5 | (num2 << 8) | (num3 << 16) | (num4 << 24)).GetAwaiter();
			WMIFunction.WMISetLogoLED(6 | (num2 << 8) | (num3 << 16) | (num4 << 24)).GetAwaiter();
			break;
		}
		if ((num & 0xFF) == 0)
		{
			return true;
		}
		return false;
	}

	public static bool set_wmi_Logo_Breath_behavior(bool state)
	{
		WMIFunction.WMISetLogoStatus(7uL | (ulong)((state ? 63 : 21) << 16)).GetAwaiter();
		return false;
	}

	public static bool set_wmi_Logo_OnOff(int index, bool state)
	{
		ulong intput = 0uL;
		switch (index)
		{
		case 1:
			intput = 1uL | (ulong)((state ? 1 : 0) << 16);
			break;
		case 2:
			intput = 6uL | (ulong)((state ? 20 : 0) << 16);
			break;
		case 3:
			intput = 0x30uL | (ulong)((state ? 6 : 0) << 24);
			break;
		}
		WMIFunction.WMISetLogoStatus(intput).GetAwaiter();
		return false;
	}

	public static int get_usb_Keyboard_Type()
	{
		uint num = USBFunction.GetUSBKeyboardType().GetAwaiter().GetResult();
		if (num == 3)
		{
			num = 0u;
		}
		return (int)num;
	}

	public static async Task<int> get_gpu_coproc_status()
	{
		int output = -1;
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			output = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 26);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return output;
		}
		catch (Exception)
		{
			return output;
		}
	}

	public static bool Get_STICKYK_Status()
	{
		uint num = 1u;
		uint num2 = 4u;
		SKEY vparam = default;
		SystemParametersInfo(58u, vparam.cbSize = 8u, ref vparam, 0u);
		SKEY sKEY = vparam;
		if ((sKEY.dwFlags & num2) == num2 && (sKEY.dwFlags & num) == num)
		{
			return true;
		}
		return false;
	}

	public static double GetCPUMaxClockSpeed()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor").Get().GetEnumerator();
			try
			{
				if (enumerator.MoveNext())
				{
					return Convert.ToDouble(((ManagementBaseObject)(ManagementObject)enumerator.Current)["MaxClockSpeed"], CultureInfo.InvariantCulture);
				}
			}
			finally
			{
				((IDisposable)enumerator)?.Dispose();
			}
		}
		catch (Exception)
		{
			return -1.0;
		}
		return -1.0;
	}

	public static bool Set_System_GPUMode(bool mode)
	{
		if ((WMIFunction.WMISetGamingMiscSetting(2uL | (ulong)(((!mode) ? 1 : 2) << 8)).GetAwaiter().GetResult() & 0xFF) == 0)
		{
			return true;
		}
		return false;
	}

	public static int Get_System_GPUMode()
	{
		ulong result = WMIFunction.WMIGetGamingMiscSetting(2u).GetAwaiter().GetResult();
		if (((result >> 8) & 0xFF) == 1 || ((result >> 8) & 0xFF) == 2 || ((result >> 8) & 0xFF) == 3)
		{
			return (int)((result >> 8) & 0xFF);
		}
		return 0;
	}

	public static int Get_GPUMode_Support()
	{
		if (((WMIFunction.WMIGetGamingMiscSetting(9u).GetAwaiter().GetResult() >> 8) & 0xFF) == 3)
		{
			return 1;
		}
		return 0;
	}

	public static int Get_TypeC_DisplayPort_Support()
	{
		ulong result = WMIFunction.WMIGetFunction(264u).GetAwaiter().GetResult();
		if ((result & 0xFF) == 0L && (int)((result >> 8) & 2) == 2)
		{
			return 1;
		}
		return 0;
	}
}
