using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using TsDotNetLib;

namespace PSCreateDefaultProfile;

internal class Program
{
	public static string gAPP_REG = "SOFTWARE\\OEM\\NitroSense";

	public static string hotkey_profile_path = "C:\\ProgramData\\oem\\NitroSense\\ProfilePool\\HotkeyProfilePool\\";

	public static string lighting_profile_path = "C:\\ProgramData\\OEM\\NitroSense\\ProfilePool\\LightProfilePool\\";

	public static uint gAssign_none_index = 6u;

	public static ResourceDictionary langRd = null;

	public static string default_content_name = "Default";

	public static string predator_exe_location = "";

	public static bool _svc_exist = false;

	public static ulong dgpu_switchable = 0uL;

	public static ulong LCD_Overdrive_support = 0uL;

	public static string[] ZoneName = new string[4] { "_Zone1", "_Zone2", "_Zone3", "_Zone4" };

	public static string NitroSense_HW_support_path_ini = "C:\\ProgramData\\oem\\NitroSense\\HW_Support.ini";

	[STAThread]
	private static void Main(string[] args)
	{
		try
		{
			string text = "en";
			try
			{
				object obj = Application.LoadComponent(new Uri("/PSCreateDefaultProfile;component/Lang/" + CultureInfo.CurrentUICulture.Parent.Name + ".xaml", UriKind.Relative));
				langRd = (ResourceDictionary)((obj is ResourceDictionary) ? obj : null);
				text = CultureInfo.CurrentUICulture.Parent.Name;
				RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\OEM\\NitroSense", writable: false);
				if (registryKey != null)
				{
					object value = registryKey.GetValue("Lang");
					if (value != null)
					{
						text = value.ToString();
						object obj2 = Application.LoadComponent(new Uri("/PSCreateDefaultProfile;component/Lang/" + text + ".xaml", UriKind.Relative));
						langRd = (ResourceDictionary)((obj2 is ResourceDictionary) ? obj2 : null);
					}
					registryKey.Close();
				}
			}
			catch
			{
				object obj4 = Application.LoadComponent(new Uri("/PSCreateDefaultProfile;component/Lang/en.xaml", UriKind.Relative));
				langRd = (ResourceDictionary)((obj4 is ResourceDictionary) ? obj4 : null);
				text = "en";
			}
			predator_exe_location = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Acer\\NitroSense\\NitroSense.exe";
			LCD_Overdrive_support = WMIAcerGamingFunction("GetGamingProfile", 0u);
			dgpu_switchable = WMIAcerGamingFunction("GetGamingMiscSetting", 9u);
			SetInitialReg();
			SetLightingInitialXML();
			SetInitialXML();
		}
		catch (Exception)
		{
		}
	}

	public static void SetInitialReg()
	{
		SetDWORDREG(gAPP_REG + "\\AdvanceSettings", "Degree", 0u);
		SetDWORDREG(gAPP_REG + "\\AdvanceSettings", "StickyKey", 0u);
		SetDWORDREG(gAPP_REG + "\\AdvanceSettings", "WinAndMenuKey", 1u);
		SetDWORDREG(gAPP_REG + "\\AdvanceSettings", "Dont_Reminder", 0u);
		SetDWORDREG(gAPP_REG + "\\FanControl", "CurrentFanMode", 0u);
		SetDWORDREG(gAPP_REG + "\\FanControl", "CPUFanCustomAuto", 1u);
		SetDWORDREG(gAPP_REG + "\\FanControl", "GPU1FanCustomAuto", 1u);
		SetDWORDREG(gAPP_REG + "\\FanControl", "CPUFanPercentage", 50u);
		SetDWORDREG(gAPP_REG + "\\FanControl", "GPU1FanPercentage", 50u);
		SetDWORDREG(gAPP_REG + "\\FanControl", "CoolBoostMode", 0u);
		SetDWORDREG(gAPP_REG + "\\Overclock", "CurrentCPUMode", 0u);
		SetDWORDREG(gAPP_REG + "\\Overclock", "CurrentGPUMode", 0u);
		SetDWORDREG(gAPP_REG + "\\Overclock", "CPU_OC_Support", 0u);
		SetDWORDREG(gAPP_REG + "\\Overclock", "GPU_OC_Support", 0u);
		SetDWORDREG(gAPP_REG, "Promotion_Information", 0u);
		SetDWORDREG(gAPP_REG + "\\Overclock", "CurrentOperationMode", 1u);
	}

	public static void SetInitialXML()
	{
		if (!Directory.Exists(hotkey_profile_path + default_content_name))
		{
			Directory.CreateDirectory(hotkey_profile_path + default_content_name);
		}
	}

	public static void SetLightingInitialXML()
	{
		if (File.Exists(lighting_profile_path + default_content_name + "\\Main.xml"))
		{
			return;
		}
		if (!Directory.Exists(lighting_profile_path + default_content_name))
		{
			Directory.CreateDirectory(lighting_profile_path + default_content_name);
		}
		string value = "#FF0000";
		string value2 = "5";
		string value3 = "5";
		string value4 = "1";
		IniFile iniFile = new IniFile(NitroSense_HW_support_path_ini);
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", "yes");
		XmlElement xmlElement = xmlDocument.CreateElement("ROOT");
		xmlElement.SetAttribute("name", default_content_name);
		xmlDocument.AppendChild(xmlElement);
		XmlElement xmlElement2 = xmlDocument.CreateElement("Key");
		xmlElement2.SetAttribute("status", "0");
		xmlElement2.SetAttribute("brightness", "5");
		xmlElement.AppendChild(xmlElement2);
		for (int i = 0; i < 127; i++)
		{
			XmlElement xmlElement3 = xmlDocument.CreateElement("Tag" + i);
			xmlElement3.SetAttribute("color", value);
			xmlElement2.AppendChild(xmlElement3);
		}
		XmlElement xmlElement4 = xmlDocument.CreateElement("Pattern");
		xmlElement4.SetAttribute("selected", "0");
		xmlElement4.SetAttribute("color", value);
		xmlElement.AppendChild(xmlElement4);
		for (int j = 0; j < 17; j++)
		{
			XmlElement xmlElement5 = xmlDocument.CreateElement("Pattern" + j);
			xmlElement5.SetAttribute("speed", value2);
			if (j == 1 || j == 3)
			{
				xmlElement5.SetAttribute("direction", "2");
			}
			else
			{
				xmlElement5.SetAttribute("direction", "0");
			}
			xmlElement4.AppendChild(xmlElement5);
		}
		XmlElement xmlElement6 = xmlDocument.CreateElement("LightingEffects");
		xmlElement6.SetAttribute("brightness", value3);
		xmlElement.AppendChild(xmlElement6);
		for (int k = 0; k < ZoneName.Count(); k++)
		{
			XmlElement xmlElement7 = xmlDocument.CreateElement("LightingEffects" + ZoneName[k]);
			xmlElement7.SetAttribute("status", value4);
			xmlElement7.SetAttribute("color", iniFile.Read("ZoneDefaultColor", "Zone" + (k + 1)));
			xmlElement6.AppendChild(xmlElement7);
		}
		xmlDocument.Save(lighting_profile_path + default_content_name + "\\Main.xml");
	}

	public static void SetStringREG(string key, string value_name, string value)
	{
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(key, writable: true);
			if (registryKey == null)
			{
				registryKey = Registry.LocalMachine.CreateSubKey(key);
			}
			if (registryKey.GetValue(value_name) == null)
			{
				registryKey.SetValue(value_name, value, RegistryValueKind.String);
			}
			if (registryKey != null)
			{
				registryKey.Close();
				registryKey = null;
			}
		}
		catch (Exception)
		{
			if (check_SVC_exist() && !Registry.ValueExistsLM(key, value_name))
			{
				Registry.SetStringLM(key, value_name, value);
				Thread.Sleep(50);
			}
		}
	}

	public static void SetDWORDREG(string key, string value_name, uint value)
	{
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(key, writable: true);
			if (registryKey == null)
			{
				registryKey = Registry.LocalMachine.CreateSubKey(key);
			}
			if (registryKey.GetValue(value_name) == null)
			{
				registryKey.SetValue(value_name, value, RegistryValueKind.DWord);
			}
			if (registryKey != null)
			{
				registryKey.Close();
				registryKey = null;
			}
		}
		catch (Exception)
		{
			if (check_SVC_exist() && !Registry.ValueExistsLM(key, value_name))
			{
				Registry.SetValueLM(key, value_name, value);
				Thread.Sleep(50);
			}
		}
	}

	private static bool check_SVC_exist()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected Obj, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		if (!_svc_exist)
		{
			for (int i = 0; i < 20; i++)
			{
				ServiceController val = new ServiceController();
				try
				{
					val.ServiceName = "PSSvc";
					if ((int)val.Status == 4)
					{
						_svc_exist = true;
						return true;
					}
					Thread.Sleep(2000);
				}
				catch (Exception)
				{
					return false;
				}
			}
			return false;
		}
		return true;
	}

	private static ulong WMIAcerGamingFunction(string function, uint dataInput)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected Obj, but got Unknown
		//IL_0025: Expected Obj, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected Obj, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		ulong result = 0uL;
		try
		{
			ManagementScope val = new ManagementScope("\\root\\wmi", (ConnectionOptions)null);
			val.Connect();
			ObjectQuery val2 = new ObjectQuery("SELECT * FROM AcerGamingFunction");
			ManagementObjectSearcher val3 = new ManagementObjectSearcher(val, val2);
			ManagementObjectEnumerator enumerator = val3.Get().GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					ManagementObject val4 = (ManagementObject)enumerator.Current;
					ManagementBaseObject methodParameters = val4.GetMethodParameters(function);
					methodParameters["gmInput"] = dataInput;
					result = Convert.ToUInt64(val4.InvokeMethod(function, methodParameters, (InvokeMethodOptions)null).Properties["gmOutput"].Value);
				}
			}
			finally
			{
				((IDisposable)enumerator)?.Dispose();
			}
			((Component)(object)val3).Dispose();
		}
		catch (Exception)
		{
		}
		return result;
	}

	private static ulong WMIAcerGetFunction(string function, uint dataInput)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected Obj, but got Unknown
		//IL_0025: Expected Obj, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected Obj, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		ulong result = 0uL;
		try
		{
			ManagementScope val = new ManagementScope("\\root\\wmi", (ConnectionOptions)null);
			val.Connect();
			ObjectQuery val2 = new ObjectQuery("SELECT * FROM APGeAction");
			ManagementObjectSearcher val3 = new ManagementObjectSearcher(val, val2);
			ManagementObjectEnumerator enumerator = val3.Get().GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					ManagementObject val4 = (ManagementObject)enumerator.Current;
					ManagementBaseObject methodParameters = val4.GetMethodParameters(function);
					methodParameters["gmInput"] = dataInput;
					result = Convert.ToUInt64(val4.InvokeMethod(function, methodParameters, (InvokeMethodOptions)null).Properties["gmOutput"].Value);
				}
			}
			finally
			{
				((IDisposable)enumerator)?.Dispose();
			}
			((Component)(object)val3).Dispose();
		}
		catch (Exception)
		{
		}
		return result;
	}
}
