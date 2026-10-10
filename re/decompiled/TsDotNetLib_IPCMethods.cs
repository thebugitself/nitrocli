using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace TsDotNetLib;

public class IPCMethods
{
	private const int CMF_CMD_INDEX = 0;

	private const int CMF_CMD_SIZE = 2;

	private const int CMF_NUM_ARG_INDEX = 2;

	private const int CMF_NUM_ARG_SIZE = 1;

	private const int CMF_ARG_SIZE_SIZE = 4;

	private const int RMF_NUM_ARG_INDEX = 0;

	private const int RMF_NUM_ARG_SIZE = 1;

	private const int RMF_ARG_SIZE_SIZE = 4;

	[DllImport("User32.dll")]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PostMessage(IntPtr hwnd, uint wMsg, UIntPtr wParam, IntPtr lParam);

	public static void SendCommandByNamedPipe(NamedPipeClientStream client, int cmdCode, params object[] args)
	{
		int num = 3;
		for (int i = 0; i < args.Length; i++)
		{
			int sizeOnMemory = GetSizeOnMemory(args[i]);
			num += 4 + sizeOnMemory;
		}
		byte[] array = new byte[num];
		int num2 = 0;
		byte[] bytes = BitConverter.GetBytes(cmdCode);
		Array.Copy(bytes, 0, array, num2, 2);
		num2 += 2;
		bytes = BitConverter.GetBytes(args.Length);
		Array.Copy(bytes, 0, array, num2, 1);
		num2++;
		for (int j = 0; j < args.Length; j++)
		{
			int sizeOnMemory2 = GetSizeOnMemory(args[j]);
			bytes = BitConverter.GetBytes(sizeOnMemory2);
			Array.Copy(bytes, 0, array, num2, 4);
			num2 += 4;
			if (sizeOnMemory2 > 0)
			{
				if (!(args[j].GetType() == typeof(string)))
				{
					bytes = ((!(args[j].GetType() == typeof(byte[]))) ? AnyToBytes(args[j]) : ((byte[])args[j]));
				}
				else
				{
					string text = (string)args[j];
					bytes = Encoding.Unicode.GetBytes(text + "\0");
				}
				Array.Copy(bytes, 0, array, num2, sizeOnMemory2);
				num2 += sizeOnMemory2;
			}
		}
		if (num != num2)
		{
			throw new Exception("Something wrong while creating command message.");
		}
		client.Write(array, 0, array.Length);
	}

	public static bool SendCommandByWindowMessage(string className, string windowName, uint message, uint wParam, int lParam)
	{
		IntPtr intPtr = FindWindow(className, windowName);
		if (intPtr == IntPtr.Zero)
		{
			return false;
		}
		return PostMessage(intPtr, message, new UIntPtr(wParam), new IntPtr(lParam));
	}

	private static int GetSizeOnMemory(object obj)
	{
		if (obj.GetType() == typeof(string))
		{
			return ((string)obj).Length * 2 + 2;
		}
		if (obj.GetType() == typeof(byte[]))
		{
			return ((byte[])obj).Length;
		}
		return Marshal.SizeOf(obj);
	}

	private static byte[] AnyToBytes(object obj)
	{
		byte[] array = new byte[Marshal.SizeOf(obj)];
		GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
		Marshal.StructureToPtr(obj, gCHandle.AddrOfPinnedObject(), fDeleteOld: false);
		gCHandle.Free();
		return array;
	}

	public static T BytesToStruct<T>(byte[] bytes)
	{
		if (bytes == null)
		{
			return default;
		}
		if (bytes.Length == 0)
		{
			return default;
		}
		int num = bytes.Length;
		IntPtr intPtr = Marshal.AllocHGlobal(num);
		try
		{
			Marshal.Copy(bytes, 0, intPtr, num);
			return (T)Marshal.PtrToStructure(intPtr, typeof(T));
		}
		catch (Exception ex)
		{
			throw new Exception("Error in BytesToStruct ! " + ex.Message);
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}
}
