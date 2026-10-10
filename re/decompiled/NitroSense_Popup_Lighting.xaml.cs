using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using TsDotNetLib;

namespace NitroSense;

public partial class Popup_Lighting : UserControl, IComponentConnector
{
	private Log _log;

	private const string LOG_REGISTRY_PATH = "SOFTWARE\\OEM\\NitroSense\\Log";

	private string[] basic_color_table = new string[9];

	private string[] g_recent_color = new string[9] { "", "", "", "", "", "", "", "", "" };

	public string current_lighting_profile = "Default";

	private string[] ZoneName = new string[4] { "_Zone1", "_Zone2", "_Zone3", "_Zone4" };

	private bool[] Zone_Status = new bool[4];

	public int BK_Hotkey_Number;

	public bool is_drag;

	private LightingDynamicUI PsLightingDynamicUIUserControl;

	public static Nitro_MainWindow_Octavia MainWindow;

	internal Rectangle main_mask_Rectangle;

	internal Grid Main_Grid;

	internal Popup colors_Popup;

	internal Path bg;

	internal Grid Basic_color_Grid;

	internal RadioButton lighting_basic_RadioButton1;

	internal RadioButton lighting_basic_RadioButton2;

	internal RadioButton lighting_basic_RadioButton3;

	internal RadioButton lighting_basic_RadioButton4;

	internal RadioButton lighting_basic_RadioButton5;

	internal RadioButton lighting_basic_RadioButton6;

	internal RadioButton lighting_basic_RadioButton7;

	internal RadioButton lighting_basic_RadioButton8;

	internal RadioButton lighting_basic_RadioButton9;

	internal Grid Lighting_recent_color_Grid;

	internal RadioButton lighting_recent_RadioButton1;

	internal RadioButton lighting_recent_RadioButton2;

	internal RadioButton lighting_recent_RadioButton3;

	internal RadioButton lighting_recent_RadioButton4;

	internal RadioButton lighting_recent_RadioButton5;

	internal RadioButton lighting_recent_RadioButton6;

	internal RadioButton lighting_recent_RadioButton7;

	internal RadioButton lighting_recent_RadioButton8;

	internal RadioButton lighting_recent_RadioButton9;

	internal Path gray;

	internal Path frame;

	internal RadioButton static_RadioButton;

	internal RadioButton dynamic_RadioButton;

	internal Path icon_low;

	internal Slider Brightness_ScrollBar;

	internal Path icon_high;

	internal Path bg_;

	internal Grid light_LightingDynamicUIUserControl_Grid;

	internal Canvas LightingDynamic;

	internal Grid Light_keyboard_On;

	internal Grid Zone4_Keyboard;

	internal Path Zone4_color_block;

	internal GradientStop Zone4_color1;

	internal GradientStop Zone4_color2;

	internal GradientStop Zone4_color3;

	internal Grid Zone3_Keyboard;

	internal Path Zone3_color_block;

	internal GradientStop Zone3_color1;

	internal GradientStop Zone3_color2;

	internal GradientStop Zone3_color3;

	internal GradientStop Zone3_color4;

	internal Grid Zone2_Keyboard;

	internal Path Zone2_color_block;

	internal GradientStop Zone2_color1;

	internal GradientStop Zone2_color2;

	internal GradientStop Zone2_color3;

	internal GradientStop Zone2_color4;

	internal Grid Zone1_Keyboard;

	internal Path Zone1_color_block;

	internal GradientStop Zone1_color1;

	internal GradientStop Zone1_color2;

	internal GradientStop Zone1_color3;

	internal Path keyboard_inner;

	internal Grid kb_cover;

	internal Path keyboard_cover;

	internal Grid Light_keyboard_Color;

	internal CheckBox Lighting_Zone1_Checkbox;

	internal Button Zone1_Color_button;

	internal Rectangle line1;

	internal CheckBox Lighting_Zone2_Checkbox;

	internal Button Zone2_Color_button;

	internal Rectangle line2;

	internal CheckBox Lighting_Zone3_Checkbox;

	internal Button Zone3_Color_button;

	internal Rectangle line3;

	internal CheckBox Lighting_Zone4_Checkbox;

	internal Button Zone4_Color_button;

	internal Button close_Button;

	public Popup_Lighting()
	{
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected Obj, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		InitializeComponent();
		((FrameworkElement)this).Resources = Startup.styled;
		BK_Hotkey_Number = Registry.CheckLM("SOFTWARE\\OEM\\NitroSense", "BK_Hotkey_Number");
		string htmlColor = new IniFile(CommonFunction.NitroSense_HW_support_path_ini).Read("ZoneDefaultColor", "Zone1");
		((Control)lighting_basic_RadioButton1).Background = (Brush)new SolidColorBrush(CommonFunction.ColorFromString(htmlColor));
		for (int i = 1; i < 10; i++)
		{
			string text = "lighting_basic_RadioButton" + i;
			object obj = ((FrameworkElement)this).FindName(text);
			Color color = ((SolidColorBrush)((Control)((obj is RadioButton) ? obj : null)).Background).Color;
			basic_color_table[i - 1] = CommonFunction.StringFromColor(color).ToString();
		}
		PsLightingDynamicUIUserControl = new LightingDynamicUI();
		((FrameworkElement)PsLightingDynamicUIUserControl).Width = 816.0;
		((FrameworkElement)PsLightingDynamicUIUserControl).Height = 392.0;
		((FrameworkElement)PsLightingDynamicUIUserControl).HorizontalAlignment = (HorizontalAlignment)1;
		((FrameworkElement)PsLightingDynamicUIUserControl).VerticalAlignment = (VerticalAlignment)0;
		Grid.SetRow((UIElement)(object)PsLightingDynamicUIUserControl, 1);
		Grid.SetColumn((UIElement)(object)PsLightingDynamicUIUserControl, 1);
		((Panel)light_LightingDynamicUIUserControl_Grid).Children.Add((UIElement)(object)PsLightingDynamicUIUserControl);
		load_lighting_profile(initial_profile_combobox: true);
	}

	private void Lighting_ScrollBar_DragStarted(object sender, DragStartedEventArgs e)
	{
		is_drag = true;
	}

	private void Lighting_ScrollBar_DragCompleted(object sender, DragCompletedEventArgs e)
	{
		is_drag = false;
		try
		{
			double num = ((RangeBase)((sender is Slider) ? sender : null)).Value;
			Lighting_ScrollBar_ValueChanged(sender, new RoutedPropertyChangedEventArgs<double>(num, num));
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Lighting_ScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (is_drag || App.initial_flag)
		{
			return;
		}
		try
		{
			current_lighting_profile = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\LightSetting", "LightingProfile");
			_ = new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile).key_group_content.tab_status;
			int num = (int)((RangeBase)((sender is Slider) ? sender : null)).Value;
			LightingProfileXML.SetLightingProfile_Zone_brightness(current_lighting_profile, num);
			if (num != 1 && num != 2 && num != 3)
			{
				_ = 4;
			}
			if (((ToggleButton)static_RadioButton).IsChecked == true)
			{
				Set_Static_Keyboard_ZoneButton_Color();
			}
			else if (((ToggleButton)dynamic_RadioButton).IsChecked == true)
			{
				PsLightingDynamicUIUserControl.Set_Dynamic_Keyboard();
			}
			Check_Brightness();
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Change_Lighting_Color_button_UI(int Button_tag, bool status)
	{
		try
		{
			if (status)
			{
				string text = "Zone" + Button_tag + "_Color_button";
				object obj = ((FrameworkElement)this).FindName(text);
				((UIElement)((obj is Button) ? obj : null)).IsEnabled = true;
			}
			else
			{
				string text2 = "Zone" + Button_tag + "_Color_button";
				object obj2 = ((FrameworkElement)this).FindName(text2);
				((UIElement)((obj2 is Button) ? obj2 : null)).IsEnabled = false;
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set UI ex = " + ex.ToString());
		}
	}

	private void Change_Lighting_Zone_Checkbox_UI(int Button_tag, bool status)
	{
		try
		{
			if (status)
			{
				string text = "Lighting_Zone" + Button_tag + "_Checkbox";
				object obj = ((FrameworkElement)this).FindName(text);
				((UIElement)((obj is CheckBox) ? obj : null)).IsEnabled = true;
			}
			else
			{
				string text2 = "Lighting_Zone" + Button_tag + "_Checkbox";
				object obj2 = ((FrameworkElement)this).FindName(text2);
				((UIElement)((obj2 is CheckBox) ? obj2 : null)).IsEnabled = false;
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set UI ex = " + ex.ToString());
		}
	}

	private void Zone_Checkbox_Checked(object sender, RoutedEventArgs e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		if (App.initial_flag)
		{
			return;
		}
		try
		{
			int num = Convert.ToInt32(((FrameworkElement)(CheckBox)sender).Tag);
			Color color = (Color)ColorConverter.ConvertFromString(new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile).lightingeffects_group_content.lightingeffects_content[num - 1].color);
			Change_Zone_Status(num, status: true);
			Change_Zone_Color(num, color);
			LightingProfileXML.SetLightingProfile_Zone_status(current_lighting_profile, num, state: true);
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Zone_Checkbox_Unchecked(object sender, RoutedEventArgs e)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (App.initial_flag)
		{
			return;
		}
		try
		{
			int num = Convert.ToInt32(((FrameworkElement)(CheckBox)sender).Tag);
			Change_Zone_Status(num, status: false);
			LightingProfileXML.SetLightingProfile_Zone_status(current_lighting_profile, num, state: false);
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Zone_ColerSetting_button_Click(object sender, RoutedEventArgs e)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected Obj, but got Unknown
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Expected Obj, but got Unknown
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Expected Obj, but got Unknown
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Expected Obj, but got Unknown
		try
		{
			object obj = ((sender is Button) ? sender : null);
			Color color = ((SolidColorBrush)((Control)obj).Background).Color;
			int num = Convert.ToInt32(((FrameworkElement)obj).Tag.ToString());
			string text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
			switch (num)
			{
			case 1:
				colors_Popup.PlacementTarget = (UIElement)(object)Zone1_Color_button;
				break;
			case 2:
				colors_Popup.PlacementTarget = (UIElement)(object)Zone2_Color_button;
				break;
			case 3:
				colors_Popup.PlacementTarget = (UIElement)(object)Zone3_Color_button;
				break;
			case 4:
				colors_Popup.PlacementTarget = (UIElement)(object)Zone4_Color_button;
				break;
			}
			load_lighting_recent_color();
			for (int i = 0; i < g_recent_color.Length; i++)
			{
				g_recent_color[i] = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\ColorRecord", "recent_color" + (i + 1));
			}
			Set_lighting_Recent_Color_RadioButton(g_recent_color);
			for (int j = 1; j <= basic_color_table.Length; j++)
			{
				if (basic_color_table[j - 1] == text)
				{
					string text2 = "lighting_basic_RadioButton" + j;
					object obj2 = ((FrameworkElement)this).FindName(text2);
					object obj3 = ((obj2 is RadioButton) ? obj2 : null);
					((ToggleButton)obj3).Checked -= Color_RadioButton_Checked;
					((ToggleButton)obj3).IsChecked = true;
					((ToggleButton)obj3).Checked += Color_RadioButton_Checked;
					break;
				}
				if (g_recent_color[j - 1] == text)
				{
					string text3 = "lighting_recent_RadioButton" + j;
					object obj4 = ((FrameworkElement)this).FindName(text3);
					object obj5 = ((obj4 is RadioButton) ? obj4 : null);
					((ToggleButton)obj5).Checked -= Color_RadioButton_Checked;
					((ToggleButton)obj5).IsChecked = true;
					((ToggleButton)obj5).Checked += Color_RadioButton_Checked;
					break;
				}
			}
			if (!colors_Popup.IsOpen)
			{
				colors_Popup.IsOpen = true;
			}
			else
			{
				colors_Popup.IsOpen = false;
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void colors_Popup_Opened(object sender, EventArgs e)
	{
		try
		{
			((UIElement)MainWindow.Mask_Rectangle).Visibility = (Visibility)0;
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void colors_Popup_Closed(object sender, EventArgs e)
	{
		try
		{
			for (int i = 1; i <= basic_color_table.Length; i++)
			{
				string text = "lighting_basic_RadioButton" + i;
				object obj = ((FrameworkElement)this).FindName(text);
				RadioButton val = (RadioButton)((obj is RadioButton) ? obj : null);
				if (((ToggleButton)val).IsChecked == true)
				{
					((ToggleButton)val).IsChecked = false;
				}
				val = null;
				text = "lighting_recent_RadioButton" + i;
				object obj2 = ((FrameworkElement)this).FindName(text);
				val = (RadioButton)((obj2 is RadioButton) ? obj2 : null);
				if (((ToggleButton)val).IsChecked == true)
				{
					((ToggleButton)val).IsChecked = false;
				}
			}
			((UIElement)MainWindow.Mask_Rectangle).Visibility = (Visibility)1;
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Color_RadioButton_Checked(object sender, RoutedEventArgs e)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			UIElement placementTarget = colors_Popup.PlacementTarget;
			Button val = (Button)placementTarget;
			int num = Convert.ToInt32(((FrameworkElement)val).Tag);
			RadioButton val2 = (RadioButton)((sender is RadioButton) ? sender : null);
			Color color = ((SolidColorBrush)((Control)val2).Background).Color;
			((object)placementTarget).GetType().GetProperty("Background").SetValue(placementTarget, ((Control)val2).Background);
			if (((FrameworkElement)val).Name.Contains("Zone"))
			{
				Change_Zone_Color(num, color);
			}
			LightingProfileXML.SetLightingProfile_Zone_color(current_lighting_profile, num, color);
			colors_Popup.IsOpen = false;
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void close_Button_Click(object sender, RoutedEventArgs e)
	{
		((UIElement)Main_Grid).Visibility = (Visibility)1;
		((UIElement)main_mask_Rectangle).Visibility = (Visibility)1;
		dynamic main_windows = App.main_windows;
		main_windows.main_mask_Rectangle.Visibility = (Visibility)1;
	}

	private void More_color_pick_Click(object sender, RoutedEventArgs e)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected Obj, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected Obj, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			((UIElement)MainWindow.main_mask_Rectangle).Visibility = (Visibility)0;
			UIElement placementTarget = colors_Popup.PlacementTarget;
			Color color = ((SolidColorBrush)((object)placementTarget).GetType().GetProperty("Background").GetValue(placementTarget)).Color;
			Button val = (Button)colors_Popup.PlacementTarget;
			int num = Convert.ToInt32(((FrameworkElement)val).Tag);
			byte r = color.R;
			byte g = color.G;
			byte b = color.B;
			More_color_picker more_color_picker = new More_color_picker(r, g, b);
			((Window)more_color_picker).Owner = (Window)(object)MainWindow;
			((Window)more_color_picker).ShowDialog();
			((UIElement)MainWindow.main_mask_Rectangle).Visibility = (Visibility)1;
			if (((Window)more_color_picker).DialogResult.Value)
			{
				ulong value = Convert.ToUInt64(more_color_picker.color_r_TextBox.Text);
				ulong value2 = Convert.ToUInt64(more_color_picker.color_g_TextBox.Text);
				ulong value3 = Convert.ToUInt64(more_color_picker.color_b_TextBox.Text);
				Color val2 = Color.FromRgb(Convert.ToByte(value), Convert.ToByte(value2), Convert.ToByte(value3));
				((object)placementTarget).GetType().GetProperty("Background").SetValue(placementTarget, (object?)new SolidColorBrush(val2));
				if (((FrameworkElement)val).Name.Contains("Zone"))
				{
					Change_Zone_Color(num, val2);
				}
				string text = $"#{val2.R:X2}{val2.G:X2}{val2.B:X2}";
				if (CheckDuplicate(text) == null)
				{
					Recent_Color_Sort(text);
					Set_lighting_Recent_Color_RadioButton(g_recent_color);
				}
				LightingProfileXML.SetLightingProfile_Zone_color(current_lighting_profile, num, val2);
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void load_lighting_recent_color()
	{
		try
		{
			for (int i = 0; i < g_recent_color.Length; i++)
			{
				g_recent_color[i] = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\ColorRecord", "recent_color" + (i + 1));
			}
			Set_lighting_Recent_Color_RadioButton(g_recent_color);
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Set_lighting_Recent_Color_RadioButton(string[] recent_color_table)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected Obj, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected Obj, but got Unknown
		try
		{
			for (int i = 0; i < recent_color_table.Length; i++)
			{
				object obj = ((FrameworkElement)this).FindName("lighting_recent_RadioButton" + (i + 1));
				RadioButton val = (RadioButton)((obj is RadioButton) ? obj : null);
				if ("" == recent_color_table[i] || recent_color_table[i] == null)
				{
					((UIElement)val).IsEnabled = false;
					((Control)val).Background = (Brush)new SolidColorBrush(Colors.Transparent);
					continue;
				}
				((UIElement)val).IsEnabled = true;
				byte b = byte.Parse(recent_color_table[i].Substring(1, 2), NumberStyles.HexNumber);
				byte b2 = byte.Parse(recent_color_table[i].Substring(3, 2), NumberStyles.HexNumber);
				byte b3 = byte.Parse(recent_color_table[i].Substring(5, 2), NumberStyles.HexNumber);
				((Control)val).Background = (Brush)new SolidColorBrush(Color.FromRgb(b, b2, b3));
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Mask_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected Obj, but got Unknown
		try
		{
			((UIElement)MainWindow.Mask_Rectangle).MouseLeftButtonDown -= Mask_Rectangle_MouseLeftButtonDown;
			((UIElement)MainWindow.Mask_Rectangle).Visibility = (Visibility)1;
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Change_Zone_Color(int Button_tag, Color color)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0459: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			switch (Button_tag)
			{
			case 1:
				Zone1_color1.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone1_color2.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone1_color3.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "00", color.R, color.G, color.B }));
				break;
			case 2:
				Zone2_color1.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "00", color.R, color.G, color.B }));
				Zone2_color2.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone2_color3.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone2_color4.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "00", color.R, color.G, color.B }));
				break;
			case 3:
				Zone3_color1.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "00", color.R, color.G, color.B }));
				Zone3_color2.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone3_color3.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone3_color4.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "00", color.R, color.G, color.B }));
				break;
			case 4:
				Zone4_color1.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "00", color.R, color.G, color.B }));
				Zone4_color2.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				Zone4_color3.Color = (Color)ColorConverter.ConvertFromString(string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", new object[4] { "FF", color.R, color.G, color.B }));
				break;
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set UI ex = " + ex.ToString());
		}
		if (App.initial_flag)
		{
			return;
		}
		try
		{
			ulong intput = 0uL;
			ulong num = Convert.ToUInt64(Math.Floor(Convert.ToDouble(color.R, CultureInfo.InvariantCulture) * CommonFunction.zone_kb_adjust[0]));
			ulong num2 = Convert.ToUInt64(Math.Floor(Convert.ToDouble(color.G, CultureInfo.InvariantCulture) * CommonFunction.zone_kb_adjust[1]));
			ulong num3 = Convert.ToUInt64(Math.Floor(Convert.ToDouble(color.B, CultureInfo.InvariantCulture) * CommonFunction.zone_kb_adjust[2]));
			switch (Button_tag)
			{
			case 1:
				intput = 1 | (num << 8) | (num2 << 16) | (num3 << 24);
				break;
			case 2:
				intput = 2 | (num << 8) | (num2 << 16) | (num3 << 24);
				break;
			case 3:
				intput = 4 | (num << 8) | (num2 << 16) | (num3 << 24);
				break;
			case 4:
				intput = 8 | (num << 8) | (num2 << 16) | (num3 << 24);
				break;
			}
			WMIFunction.WMISetGamingRgbKbSetting(intput).GetAwaiter();
		}
		catch (Exception ex2)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex2.ToString());
		}
	}

	private void Change_Zone_Status(int Button_tag, bool status)
	{
		try
		{
			if (status)
			{
				string text = "Zone" + Button_tag + "_Color_button";
				object obj = ((FrameworkElement)this).FindName(text);
				((UIElement)((obj is Button) ? obj : null)).IsEnabled = true;
				string text2 = "Zone" + Button_tag + "_Keyboard";
				object obj2 = ((FrameworkElement)this).FindName(text2);
				((UIElement)((obj2 is Grid) ? obj2 : null)).Visibility = (Visibility)0;
			}
			else
			{
				string text3 = "Zone" + Button_tag + "_Color_button";
				object obj3 = ((FrameworkElement)this).FindName(text3);
				((UIElement)((obj3 is Button) ? obj3 : null)).IsEnabled = false;
				string text4 = "Zone" + Button_tag + "_Keyboard";
				object obj4 = ((FrameworkElement)this).FindName(text4);
				((UIElement)((obj4 is Grid) ? obj4 : null)).Visibility = (Visibility)2;
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set UI ex = " + ex.ToString());
		}
		if (App.initial_flag)
		{
			return;
		}
		try
		{
			WMIFunction.WMISetGamingLEDBehavior((ulong)(8 | ((((ToggleButton)Lighting_Zone1_Checkbox).IsChecked == true) ? 1099511627776L : 0) | ((((ToggleButton)Lighting_Zone2_Checkbox).IsChecked == true) ? 2199023255552L : 0) | ((((ToggleButton)Lighting_Zone3_Checkbox).IsChecked == true) ? 4398046511104L : 0) | ((((ToggleButton)Lighting_Zone4_Checkbox).IsChecked == true) ? 8796093022208L : 0))).GetAwaiter().GetResult();
		}
		catch (Exception ex2)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set WMI ex = " + ex2.ToString());
		}
	}

	private void Change_Zone_UI(int Button_tag, bool status)
	{
		try
		{
			if (status)
			{
				string text = "Zone" + Button_tag + "_Color_button";
				object obj = ((FrameworkElement)this).FindName(text);
				((UIElement)((obj is Button) ? obj : null)).IsEnabled = true;
				string text2 = "Zone" + Button_tag + "_Keyboard";
				object obj2 = ((FrameworkElement)this).FindName(text2);
				((UIElement)((obj2 is Grid) ? obj2 : null)).Visibility = (Visibility)0;
			}
			else
			{
				string text3 = "Zone" + Button_tag + "_Color_button";
				object obj3 = ((FrameworkElement)this).FindName(text3);
				((UIElement)((obj3 is Button) ? obj3 : null)).IsEnabled = false;
				string text4 = "Zone" + Button_tag + "_Keyboard";
				object obj4 = ((FrameworkElement)this).FindName(text4);
				((UIElement)((obj4 is Grid) ? obj4 : null)).Visibility = (Visibility)2;
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set UI ex = " + ex.ToString());
		}
		try
		{
			if (status)
			{
				string text5 = "Zone" + Button_tag + "_Color_button_g";
				object obj5 = ((FrameworkElement)this).FindName(text5);
				((UIElement)((obj5 is Button) ? obj5 : null)).IsEnabled = true;
				string text6 = "Zone" + Button_tag + "_Keyboard_g";
				object obj6 = ((FrameworkElement)this).FindName(text6);
				((UIElement)((obj6 is Grid) ? obj6 : null)).Visibility = (Visibility)0;
			}
			else
			{
				string text7 = "Zone" + Button_tag + "_Color_button_g";
				object obj7 = ((FrameworkElement)this).FindName(text7);
				((UIElement)((obj7 is Button) ? obj7 : null)).IsEnabled = false;
				string text8 = "Zone" + Button_tag + "_Keyboard_g";
				object obj8 = ((FrameworkElement)this).FindName(text8);
				((UIElement)((obj8 is Grid) ? obj8 : null)).Visibility = (Visibility)2;
			}
		}
		catch (Exception ex2)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set UI ex = " + ex2.ToString());
		}
	}

	private void Change_Zone_Brightness(int brightness)
	{
		double num = brightness;
		((UIElement)Zone1_Keyboard).Opacity = num / 100.0;
		((UIElement)Zone2_Keyboard).Opacity = num / 100.0;
		((UIElement)Zone3_Keyboard).Opacity = num / 100.0;
		((UIElement)Zone4_Keyboard).Opacity = num / 100.0;
		if (!App.initial_flag)
		{
			ulong num2 = 0uL;
			num2 = brightness switch
			{
				33 => 39uL, 
				66 => 84uL, 
				100 => 100uL, 
				_ => 84uL, 
			};
			ulong num3 = WMIFunction.WMIGetFunction((uint)(1 | ((long)BK_Hotkey_Number << 8) | 0xFFFF0000u)).GetAwaiter().GetResult() >> 16;
			WMIFunction.WMISetFunction((ulong)(2 | ((long)BK_Hotkey_Number << 8)) | (num3 << 16) | (num2 << 32)).GetAwaiter();
		}
	}

	private void Change_Keyboard_Brightness(int brightness)
	{
		if (!App.initial_flag)
		{
			WMIFunction.WMISetGamingKBBacklight((ulong)(0 | ((long)((brightness - 1) * 25) << 16) | 0 | 0 | 0 | 0 | 0)).GetAwaiter();
		}
	}

	private void Change_Zone_Brightness_RGB(int brightness)
	{
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		double num = brightness;
		((UIElement)Zone1_Keyboard).Opacity = num / 100.0;
		((UIElement)Zone2_Keyboard).Opacity = num / 100.0;
		((UIElement)Zone3_Keyboard).Opacity = num / 100.0;
		((UIElement)Zone4_Keyboard).Opacity = num / 100.0;
		if (App.initial_flag)
		{
			return;
		}
		ulong intput = 0uL;
		double num2 = 0.0;
		num2 = brightness switch
		{
			0 => 0.0, 
			50 => 0.5, 
			100 => 1.0, 
			_ => 1.0, 
		};
		try
		{
			LightingProfileXML lightingProfileXML = new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile);
			for (int i = 0; i < ZoneName.Count(); i++)
			{
				Color val = (Color)ColorConverter.ConvertFromString(lightingProfileXML.lightingeffects_group_content.lightingeffects_content[i].color);
				ulong num3 = Convert.ToUInt64(Math.Floor(Convert.ToDouble(val.R, CultureInfo.InvariantCulture) * CommonFunction.zone_kb_adjust[0] * num2));
				ulong num4 = Convert.ToUInt64(Math.Floor(Convert.ToDouble(val.G, CultureInfo.InvariantCulture) * CommonFunction.zone_kb_adjust[1] * num2));
				ulong num5 = Convert.ToUInt64(Math.Floor(Convert.ToDouble(val.B, CultureInfo.InvariantCulture) * CommonFunction.zone_kb_adjust[2] * num2));
				if (i + 1 == 1)
				{
					intput = 1 | (num3 << 8) | (num4 << 16) | (num5 << 24);
				}
				else if (i + 1 == 2)
				{
					intput = 2 | (num3 << 8) | (num4 << 16) | (num5 << 24);
				}
				else if (i + 1 == 3)
				{
					intput = 4 | (num3 << 8) | (num4 << 16) | (num5 << 24);
				}
				else if (i + 1 == 4)
				{
					intput = 8 | (num3 << 8) | (num4 << 16) | (num5 << 24);
				}
				WMIFunction.WMISetGamingRgbKbSetting(intput).GetAwaiter();
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void Recent_Color_Sort(string new_recent_color)
	{
		try
		{
			string[] array = g_recent_color;
			bool flag = false;
			for (int i = 0; i < g_recent_color.Length; i++)
			{
				if (new_recent_color == g_recent_color[i])
				{
					for (int num = i; num > 0; num--)
					{
						g_recent_color[num] = array[num - 1];
					}
					g_recent_color[0] = new_recent_color;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				for (int num2 = g_recent_color.Length - 1; num2 > 0; num2--)
				{
					g_recent_color[num2] = array[num2 - 1];
				}
				g_recent_color[0] = new_recent_color;
			}
			for (int j = 0; j < g_recent_color.Length; j++)
			{
				Registry.SetStringLM("SOFTWARE\\OEM\\NitroSense\\ColorRecord", "recent_color" + (j + 1), g_recent_color[j]);
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private RadioButton CheckDuplicate(string Check_Color)
	{
		try
		{
			for (int i = 0; i < basic_color_table.Length; i++)
			{
				if (basic_color_table[i] == Check_Color)
				{
					object obj = ((FrameworkElement)this).FindName("lighting_basic_RadioButton" + (i + 1));
					object obj2 = ((obj is RadioButton) ? obj : null);
					((ToggleButton)obj2).IsChecked = true;
					return (RadioButton)obj2;
				}
			}
			for (int j = 0; j < g_recent_color.Length; j++)
			{
				if (g_recent_color[j] == Check_Color)
				{
					object obj3 = ((FrameworkElement)this).FindName("lighting_recent_RadioButton" + (j + 1));
					object obj4 = ((obj3 is RadioButton) ? obj3 : null);
					((ToggleButton)obj4).IsChecked = true;
					return (RadioButton)obj4;
				}
			}
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
		return null;
	}

	public void load_lighting_profile(bool initial_profile_combobox)
	{
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected Obj, but got Unknown
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Expected Obj, but got Unknown
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Expected Obj, but got Unknown
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Expected Obj, but got Unknown
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Expected Obj, but got Unknown
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		load_lighting_recent_color();
		if (!Registry.ValueExistsLM("SOFTWARE\\OEM\\NitroSense\\LightSetting", "LightingProfile"))
		{
			return;
		}
		current_lighting_profile = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\LightSetting", "LightingProfile");
		if (!Directory.Exists(CommonFunction.lighting_profile_path + current_lighting_profile))
		{
			return;
		}
		try
		{
			LightingProfileXML lightingProfileXML = new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile);
			if (lightingProfileXML.key_group_content.tab_status == 0)
			{
				if ((!((ToggleButton)static_RadioButton).IsChecked).Value)
				{
					((ToggleButton)static_RadioButton).IsChecked = true;
				}
			}
			else if ((!((ToggleButton)dynamic_RadioButton).IsChecked).Value)
			{
				((ToggleButton)dynamic_RadioButton).IsChecked = true;
			}
			int brightness = lightingProfileXML.lightingeffects_group_content.brightness;
			((RangeBase)Brightness_ScrollBar).Value = brightness;
			for (int i = 0; i < ZoneName.Count(); i++)
			{
				bool flag = lightingProfileXML.lightingeffects_group_content.lightingeffects_content[i].status.Equals(1);
				Color val = (Color)ColorConverter.ConvertFromString(lightingProfileXML.lightingeffects_group_content.lightingeffects_content[i].color);
				string text = "";
				text = "Lighting" + ZoneName[i] + "_Checkbox";
				object obj = ((FrameworkElement)this).FindName(text);
				object obj2 = ((obj is CheckBox) ? obj : null);
				((ToggleButton)obj2).Checked -= Zone_Checkbox_Checked;
				((ToggleButton)obj2).Unchecked -= Zone_Checkbox_Unchecked;
				((ToggleButton)obj2).IsChecked = flag;
				((ToggleButton)obj2).Checked += Zone_Checkbox_Checked;
				((ToggleButton)obj2).Unchecked += Zone_Checkbox_Unchecked;
				Change_Zone_UI(i + 1, flag);
				string text2 = "";
				text2 = "Zone" + (i + 1) + "_Color_button";
				object obj3 = ((FrameworkElement)this).FindName(text2);
				((Control)((obj3 is Button) ? obj3 : null)).Background = (Brush)new SolidColorBrush(val);
				if (flag)
				{
					Change_Zone_Color(i + 1, val);
				}
				Zone_Status[i] = flag;
			}
			try
			{
				WMIFunction.WMISetGamingLEDBehavior((ulong)(8 | (Zone_Status[0] ? 1099511627776L : 0) | (Zone_Status[1] ? 2199023255552L : 0) | (Zone_Status[2] ? 4398046511104L : 0) | (Zone_Status[3] ? 8796093022208L : 0))).GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set WMI ex = " + ex.ToString());
			}
			Check_Brightness();
		}
		catch (Exception ex2)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex2.ToString());
		}
	}

	public void load_lighting_profile(string profile_name, bool initial_profile_combobox)
	{
		load_lighting_recent_color();
		current_lighting_profile = profile_name;
		if (!Directory.Exists(CommonFunction.lighting_profile_path + current_lighting_profile))
		{
			return;
		}
		try
		{
			if (new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile).key_group_content.tab_status == 0)
			{
				if ((!((ToggleButton)static_RadioButton).IsChecked).Value)
				{
					((ToggleButton)static_RadioButton).IsChecked = true;
				}
				Set_Static_Keyboard_Set_Status();
				Set_Static_Keyboard_ZoneButton_Color();
			}
			else
			{
				if ((!((ToggleButton)dynamic_RadioButton).IsChecked).Value)
				{
					((ToggleButton)dynamic_RadioButton).IsChecked = true;
				}
				PsLightingDynamicUIUserControl.Set_Dynamic_Keyboard();
				PsLightingDynamicUIUserControl.load_lighting_recent_color();
			}
			Check_Brightness();
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "ex = " + ex.ToString());
		}
	}

	private void static_RadioButton_Checked(object sender, RoutedEventArgs e)
	{
		((UIElement)light_LightingDynamicUIUserControl_Grid).Visibility = (Visibility)1;
		((UIElement)Light_keyboard_Color).Visibility = (Visibility)0;
		((UIElement)Light_keyboard_On).Visibility = (Visibility)0;
		LightingProfileXML.SetLightingProfile_Key_status(current_lighting_profile, 0);
		if (!App.initial_flag)
		{
			Set_Static_Keyboard_Set_Status();
			Set_Static_Keyboard_ZoneButton_Color();
		}
	}

	private void dynamic_RadioButton_Checked(object sender, RoutedEventArgs e)
	{
		((UIElement)light_LightingDynamicUIUserControl_Grid).Visibility = (Visibility)0;
		((UIElement)Light_keyboard_Color).Visibility = (Visibility)1;
		((UIElement)Light_keyboard_On).Visibility = (Visibility)1;
		LightingProfileXML.SetLightingProfile_Key_status(current_lighting_profile, 1);
		if (!App.initial_flag)
		{
			PsLightingDynamicUIUserControl.Set_Dynamic_Keyboard();
			PsLightingDynamicUIUserControl.load_lighting_recent_color();
			PsLightingDynamicUIUserControl.load_lighting_profile(initial_profile_combobox: true);
		}
	}

	private void Set_Static_Keyboard_Set_Status()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected Obj, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected Obj, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected Obj, but got Unknown
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected Obj, but got Unknown
		int num = 4;
		for (int i = 0; i < num; i++)
		{
			current_lighting_profile = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\LightSetting", "LightingProfile");
			bool flag = new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile).lightingeffects_group_content.lightingeffects_content[i].status.Equals(1);
			string text = "";
			text = "Lighting" + ZoneName[i] + "_Checkbox";
			object obj = ((FrameworkElement)this).FindName(text);
			object obj2 = ((obj is CheckBox) ? obj : null);
			((ToggleButton)obj2).Checked -= Zone_Checkbox_Checked;
			((ToggleButton)obj2).Unchecked -= Zone_Checkbox_Unchecked;
			((ToggleButton)obj2).IsChecked = flag;
			((ToggleButton)obj2).Checked += Zone_Checkbox_Checked;
			((ToggleButton)obj2).Unchecked += Zone_Checkbox_Unchecked;
			Zone_Status[i] = flag;
			Change_Zone_UI(i + 1, flag);
		}
		try
		{
			WMIFunction.WMISetGamingLEDBehavior((ulong)(8 | (Zone_Status[0] ? 1099511627776L : 0) | (Zone_Status[1] ? 2199023255552L : 0) | (Zone_Status[2] ? 4398046511104L : 0) | (Zone_Status[3] ? 8796093022208L : 0))).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			Log.LogWrite(_log, LogType.Exception, MethodBase.GetCurrentMethod().Name, "Set WMI ex = " + ex.ToString());
		}
	}

	private void Set_Static_Keyboard_ZoneButton_Color()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected Obj, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		if (App.initial_flag)
		{
			return;
		}
		current_lighting_profile = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\LightSetting", "LightingProfile");
		LightingProfileXML lightingProfileXML = new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile);
		int brightness = lightingProfileXML.lightingeffects_group_content.brightness;
		((RangeBase)Brightness_ScrollBar).Value = brightness;
		Change_Keyboard_Brightness(brightness);
		int num = 4;
		for (int i = 0; i < num; i++)
		{
			bool flag = lightingProfileXML.lightingeffects_group_content.lightingeffects_content[i].status.Equals(1);
			Color val = (Color)ColorConverter.ConvertFromString(lightingProfileXML.lightingeffects_group_content.lightingeffects_content[i].color);
			string text = "";
			text = "Zone" + (i + 1) + "_Color_button";
			object obj = ((FrameworkElement)this).FindName(text);
			((Control)((obj is Button) ? obj : null)).Background = (Brush)new SolidColorBrush(val);
			if (flag)
			{
				Change_Zone_Color(i + 1, val);
			}
		}
		Check_Brightness();
	}

	private void Check_Brightness()
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		current_lighting_profile = Registry.GetStringLM("SOFTWARE\\OEM\\NitroSense\\LightSetting", "LightingProfile");
		LightingProfileXML lightingProfileXML = new LightingProfileXML(CommonFunction.lighting_profile_path + current_lighting_profile);
		int brightness = lightingProfileXML.lightingeffects_group_content.brightness;
		int num = 4;
		if (brightness == 1)
		{
			((UIElement)PsLightingDynamicUIUserControl).Opacity = 0.4;
			PsLightingDynamicUIUserControl.Control_all_color_radiobutton(isNeedEnable: false);
			PsLightingDynamicUIUserControl.Control_all_effect_radiobutton(isNeedEnable: false);
			((UIElement)Light_keyboard_Color).Opacity = 0.4;
			((UIElement)kb_cover).Visibility = (Visibility)0;
			if (Startup._feature_type == 2)
			{
				((FrameworkElement)keyboard_cover).Margin = new Thickness(1.5, 31.666, 0.0, -30.267);
			}
			for (int i = 1; i <= num; i++)
			{
				Change_Lighting_Color_button_UI(i, status: false);
				Change_Lighting_Zone_Checkbox_UI(i, status: false);
			}
		}
		else
		{
			((UIElement)PsLightingDynamicUIUserControl).Opacity = 1.0;
			PsLightingDynamicUIUserControl.Control_all_effect_radiobutton(isNeedEnable: true);
			PsLightingDynamicUIUserControl.check_color_setting();
			((UIElement)kb_cover).Visibility = (Visibility)1;
			((UIElement)Light_keyboard_Color).Opacity = 1.0;
			for (int j = 0; j < num; j++)
			{
				bool status = lightingProfileXML.lightingeffects_group_content.lightingeffects_content[j].status.Equals(1);
				Change_Lighting_Color_button_UI(j + 1, status);
				Change_Lighting_Zone_Checkbox_UI(j + 1, status: true);
			}
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return (AutomationPeer)(object)new MyPopup_LightingAutomationPeer(this);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Expected Obj, but got Unknown
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Expected Obj, but got Unknown
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Expected Obj, but got Unknown
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Expected Obj, but got Unknown
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Expected Obj, but got Unknown
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Expected Obj, but got Unknown
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected Obj, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Expected Obj, but got Unknown
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected Obj, but got Unknown
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Expected Obj, but got Unknown
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Expected Obj, but got Unknown
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Expected Obj, but got Unknown
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Expected Obj, but got Unknown
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Expected Obj, but got Unknown
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Expected Obj, but got Unknown
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Expected Obj, but got Unknown
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Expected Obj, but got Unknown
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Expected Obj, but got Unknown
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Expected Obj, but got Unknown
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected Obj, but got Unknown
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Expected Obj, but got Unknown
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Expected Obj, but got Unknown
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Expected Obj, but got Unknown
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Expected Obj, but got Unknown
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0301: Expected Obj, but got Unknown
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Expected Obj, but got Unknown
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Expected Obj, but got Unknown
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Expected Obj, but got Unknown
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Expected Obj, but got Unknown
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Expected Obj, but got Unknown
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Expected Obj, but got Unknown
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Expected Obj, but got Unknown
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Expected Obj, but got Unknown
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Expected Obj, but got Unknown
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Expected Obj, but got Unknown
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Expected Obj, but got Unknown
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Expected Obj, but got Unknown
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Expected Obj, but got Unknown
		//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Expected Obj, but got Unknown
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Expected Obj, but got Unknown
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Expected Obj, but got Unknown
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Expected Obj, but got Unknown
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Expected Obj, but got Unknown
		//IL_0453: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Expected Obj, but got Unknown
		//IL_0460: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Expected Obj, but got Unknown
		//IL_046d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Expected Obj, but got Unknown
		//IL_0484: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Expected Obj, but got Unknown
		//IL_0491: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Expected Obj, but got Unknown
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Expected Obj, but got Unknown
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Expected Obj, but got Unknown
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cc: Expected Obj, but got Unknown
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Expected Obj, but got Unknown
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0504: Expected Obj, but got Unknown
		//IL_051e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Expected Obj, but got Unknown
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Expected Obj, but got Unknown
		//IL_0538: Unknown result type (might be due to invalid IL or missing references)
		//IL_0542: Expected Obj, but got Unknown
		//IL_0545: Unknown result type (might be due to invalid IL or missing references)
		//IL_054f: Expected Obj, but got Unknown
		//IL_0552: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Expected Obj, but got Unknown
		//IL_055f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Expected Obj, but got Unknown
		//IL_056c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Expected Obj, but got Unknown
		//IL_0579: Unknown result type (might be due to invalid IL or missing references)
		//IL_0583: Expected Obj, but got Unknown
		//IL_0586: Unknown result type (might be due to invalid IL or missing references)
		//IL_0590: Expected Obj, but got Unknown
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_059d: Expected Obj, but got Unknown
		//IL_05a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05aa: Expected Obj, but got Unknown
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b7: Expected Obj, but got Unknown
		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c4: Expected Obj, but got Unknown
		//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d1: Expected Obj, but got Unknown
		//IL_05d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05de: Expected Obj, but got Unknown
		//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05eb: Expected Obj, but got Unknown
		//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f8: Expected Obj, but got Unknown
		//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0605: Expected Obj, but got Unknown
		//IL_0608: Unknown result type (might be due to invalid IL or missing references)
		//IL_0612: Expected Obj, but got Unknown
		//IL_0615: Unknown result type (might be due to invalid IL or missing references)
		//IL_061f: Expected Obj, but got Unknown
		//IL_0622: Unknown result type (might be due to invalid IL or missing references)
		//IL_062c: Expected Obj, but got Unknown
		//IL_062f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0639: Expected Obj, but got Unknown
		//IL_063c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0646: Expected Obj, but got Unknown
		//IL_0649: Unknown result type (might be due to invalid IL or missing references)
		//IL_0653: Expected Obj, but got Unknown
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_0660: Expected Obj, but got Unknown
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Expected Obj, but got Unknown
		//IL_0670: Unknown result type (might be due to invalid IL or missing references)
		//IL_067a: Expected Obj, but got Unknown
		//IL_067d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0687: Expected Obj, but got Unknown
		//IL_068a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0694: Expected Obj, but got Unknown
		//IL_0697: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a1: Expected Obj, but got Unknown
		//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ae: Expected Obj, but got Unknown
		//IL_06b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bb: Expected Obj, but got Unknown
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d2: Expected Obj, but got Unknown
		//IL_06df: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e9: Expected Obj, but got Unknown
		//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f6: Expected Obj, but got Unknown
		//IL_0703: Unknown result type (might be due to invalid IL or missing references)
		//IL_070d: Expected Obj, but got Unknown
		//IL_0710: Unknown result type (might be due to invalid IL or missing references)
		//IL_071a: Expected Obj, but got Unknown
		//IL_071d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0727: Expected Obj, but got Unknown
		//IL_0734: Unknown result type (might be due to invalid IL or missing references)
		//IL_073e: Expected Obj, but got Unknown
		//IL_074b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0755: Expected Obj, but got Unknown
		//IL_0758: Unknown result type (might be due to invalid IL or missing references)
		//IL_0762: Expected Obj, but got Unknown
		//IL_076f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0779: Expected Obj, but got Unknown
		//IL_077c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0786: Expected Obj, but got Unknown
		//IL_0789: Unknown result type (might be due to invalid IL or missing references)
		//IL_0793: Expected Obj, but got Unknown
		//IL_07a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07aa: Expected Obj, but got Unknown
		//IL_07b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c1: Expected Obj, but got Unknown
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ce: Expected Obj, but got Unknown
		//IL_07db: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e5: Expected Obj, but got Unknown
		//IL_07e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f2: Expected Obj, but got Unknown
		//IL_07f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ff: Expected Obj, but got Unknown
		//IL_080c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0816: Expected Obj, but got Unknown
		//IL_0823: Unknown result type (might be due to invalid IL or missing references)
		//IL_082d: Expected Obj, but got Unknown
		//IL_0830: Unknown result type (might be due to invalid IL or missing references)
		//IL_083a: Expected Obj, but got Unknown
		//IL_0847: Unknown result type (might be due to invalid IL or missing references)
		//IL_0851: Expected Obj, but got Unknown
		//IL_0854: Unknown result type (might be due to invalid IL or missing references)
		//IL_085e: Expected Obj, but got Unknown
		//IL_086b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0875: Expected Obj, but got Unknown
		switch (connectionId)
		{
		case 1:
			main_mask_Rectangle = (Rectangle)target;
			break;
		case 2:
			Main_Grid = (Grid)target;
			break;
		case 3:
			colors_Popup = (Popup)target;
			colors_Popup.Opened += colors_Popup_Opened;
			colors_Popup.Closed += colors_Popup_Closed;
			break;
		case 4:
			bg = (Path)target;
			break;
		case 5:
			Basic_color_Grid = (Grid)target;
			break;
		case 6:
			lighting_basic_RadioButton1 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton1).Checked += Color_RadioButton_Checked;
			break;
		case 7:
			lighting_basic_RadioButton2 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton2).Checked += Color_RadioButton_Checked;
			break;
		case 8:
			lighting_basic_RadioButton3 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton3).Checked += Color_RadioButton_Checked;
			break;
		case 9:
			lighting_basic_RadioButton4 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton4).Checked += Color_RadioButton_Checked;
			break;
		case 10:
			lighting_basic_RadioButton5 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton5).Checked += Color_RadioButton_Checked;
			break;
		case 11:
			lighting_basic_RadioButton6 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton6).Checked += Color_RadioButton_Checked;
			break;
		case 12:
			lighting_basic_RadioButton7 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton7).Checked += Color_RadioButton_Checked;
			break;
		case 13:
			lighting_basic_RadioButton8 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton8).Checked += Color_RadioButton_Checked;
			break;
		case 14:
			lighting_basic_RadioButton9 = (RadioButton)target;
			((ToggleButton)lighting_basic_RadioButton9).Checked += Color_RadioButton_Checked;
			break;
		case 15:
			Lighting_recent_color_Grid = (Grid)target;
			break;
		case 16:
			lighting_recent_RadioButton1 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton1).Checked += Color_RadioButton_Checked;
			break;
		case 17:
			lighting_recent_RadioButton2 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton2).Checked += Color_RadioButton_Checked;
			break;
		case 18:
			lighting_recent_RadioButton3 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton3).Checked += Color_RadioButton_Checked;
			break;
		case 19:
			lighting_recent_RadioButton4 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton4).Checked += Color_RadioButton_Checked;
			break;
		case 20:
			lighting_recent_RadioButton5 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton5).Checked += Color_RadioButton_Checked;
			break;
		case 21:
			lighting_recent_RadioButton6 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton6).Checked += Color_RadioButton_Checked;
			break;
		case 22:
			lighting_recent_RadioButton7 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton7).Checked += Color_RadioButton_Checked;
			break;
		case 23:
			lighting_recent_RadioButton8 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton8).Checked += Color_RadioButton_Checked;
			break;
		case 24:
			lighting_recent_RadioButton9 = (RadioButton)target;
			((ToggleButton)lighting_recent_RadioButton9).Checked += Color_RadioButton_Checked;
			break;
		case 25:
			((ButtonBase)(Button)target).Click += More_color_pick_Click;
			break;
		case 26:
			gray = (Path)target;
			break;
		case 27:
			frame = (Path)target;
			break;
		case 28:
			static_RadioButton = (RadioButton)target;
			((ToggleButton)static_RadioButton).Checked += static_RadioButton_Checked;
			break;
		case 29:
			dynamic_RadioButton = (RadioButton)target;
			((ToggleButton)dynamic_RadioButton).Checked += dynamic_RadioButton_Checked;
			break;
		case 30:
			icon_low = (Path)target;
			break;
		case 31:
			Brightness_ScrollBar = (Slider)target;
			((UIElement)Brightness_ScrollBar).AddHandler(Thumb.DragStartedEvent, (Delegate)new DragStartedEventHandler(Lighting_ScrollBar_DragStarted));
			((UIElement)Brightness_ScrollBar).AddHandler(Thumb.DragCompletedEvent, (Delegate)new DragCompletedEventHandler(Lighting_ScrollBar_DragCompleted));
			((RangeBase)Brightness_ScrollBar).ValueChanged += Lighting_ScrollBar_ValueChanged;
			break;
		case 32:
			icon_high = (Path)target;
			break;
		case 33:
			bg_ = (Path)target;
			break;
		case 34:
			light_LightingDynamicUIUserControl_Grid = (Grid)target;
			break;
		case 35:
			LightingDynamic = (Canvas)target;
			break;
		case 36:
			Light_keyboard_On = (Grid)target;
			break;
		case 37:
			Zone4_Keyboard = (Grid)target;
			break;
		case 38:
			Zone4_color_block = (Path)target;
			break;
		case 39:
			Zone4_color1 = (GradientStop)target;
			break;
		case 40:
			Zone4_color2 = (GradientStop)target;
			break;
		case 41:
			Zone4_color3 = (GradientStop)target;
			break;
		case 42:
			Zone3_Keyboard = (Grid)target;
			break;
		case 43:
			Zone3_color_block = (Path)target;
			break;
		case 44:
			Zone3_color1 = (GradientStop)target;
			break;
		case 45:
			Zone3_color2 = (GradientStop)target;
			break;
		case 46:
			Zone3_color3 = (GradientStop)target;
			break;
		case 47:
			Zone3_color4 = (GradientStop)target;
			break;
		case 48:
			Zone2_Keyboard = (Grid)target;
			break;
		case 49:
			Zone2_color_block = (Path)target;
			break;
		case 50:
			Zone2_color1 = (GradientStop)target;
			break;
		case 51:
			Zone2_color2 = (GradientStop)target;
			break;
		case 52:
			Zone2_color3 = (GradientStop)target;
			break;
		case 53:
			Zone2_color4 = (GradientStop)target;
			break;
		case 54:
			Zone1_Keyboard = (Grid)target;
			break;
		case 55:
			Zone1_color_block = (Path)target;
			break;
		case 56:
			Zone1_color1 = (GradientStop)target;
			break;
		case 57:
			Zone1_color2 = (GradientStop)target;
			break;
		case 58:
			Zone1_color3 = (GradientStop)target;
			break;
		case 59:
			keyboard_inner = (Path)target;
			break;
		case 60:
			kb_cover = (Grid)target;
			break;
		case 61:
			keyboard_cover = (Path)target;
			break;
		case 62:
			Light_keyboard_Color = (Grid)target;
			break;
		case 63:
			Lighting_Zone1_Checkbox = (CheckBox)target;
			((ToggleButton)Lighting_Zone1_Checkbox).Checked += Zone_Checkbox_Checked;
			((ToggleButton)Lighting_Zone1_Checkbox).Unchecked += Zone_Checkbox_Unchecked;
			break;
		case 64:
			Zone1_Color_button = (Button)target;
			((ButtonBase)Zone1_Color_button).Click += Zone_ColerSetting_button_Click;
			break;
		case 65:
			line1 = (Rectangle)target;
			break;
		case 66:
			Lighting_Zone2_Checkbox = (CheckBox)target;
			((ToggleButton)Lighting_Zone2_Checkbox).Checked += Zone_Checkbox_Checked;
			((ToggleButton)Lighting_Zone2_Checkbox).Unchecked += Zone_Checkbox_Unchecked;
			break;
		case 67:
			Zone2_Color_button = (Button)target;
			((ButtonBase)Zone2_Color_button).Click += Zone_ColerSetting_button_Click;
			break;
		case 68:
			line2 = (Rectangle)target;
			break;
		case 69:
			Lighting_Zone3_Checkbox = (CheckBox)target;
			((ToggleButton)Lighting_Zone3_Checkbox).Checked += Zone_Checkbox_Checked;
			((ToggleButton)Lighting_Zone3_Checkbox).Unchecked += Zone_Checkbox_Unchecked;
			break;
		case 70:
			Zone3_Color_button = (Button)target;
			((ButtonBase)Zone3_Color_button).Click += Zone_ColerSetting_button_Click;
			break;
		case 71:
			line3 = (Rectangle)target;
			break;
		case 72:
			Lighting_Zone4_Checkbox = (CheckBox)target;
			((ToggleButton)Lighting_Zone4_Checkbox).Checked += Zone_Checkbox_Checked;
			((ToggleButton)Lighting_Zone4_Checkbox).Unchecked += Zone_Checkbox_Unchecked;
			break;
		case 73:
			Zone4_Color_button = (Button)target;
			((ButtonBase)Zone4_Color_button).Click += Zone_ColerSetting_button_Click;
			break;
		case 74:
			close_Button = (Button)target;
			((ButtonBase)close_Button).Click += close_Button_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
