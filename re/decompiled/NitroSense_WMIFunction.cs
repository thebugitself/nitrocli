using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using TsDotNetLib;

namespace NitroSense;

public class WMIFunction
{
	public static async Task<uint> SetAcerGamingPorfileConfiguration(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 9, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<ulong> GetAcerGamingPorfileConfiguration(uint intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			ulong result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 10, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[13];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt64(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return 4294967295uL;
		}
	}

	public static async Task<uint> SetAcerGamingLEDGroupColor(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 11, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<ulong> GetAcerGamingLEDGroupColor(uint intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			ulong result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 12, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[13];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt64(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return 4294967295uL;
		}
	}

	public static async Task<ulong> GetAcerGamingSystemInformation(uint intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			ulong result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 13, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[13];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt64(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return 4294967295uL;
		}
	}

	public static async Task<uint> SetAcerGamingFanGroupBehavior(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 15, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> SetAcerGamingFanGroupSpeed(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 16, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> WMISetFunction(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 17, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<ulong> WMIGetFunction(uint intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			ulong result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 20, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[13];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt64(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return 4294967295uL;
		}
	}

	public static async Task<uint> WMISetLogoLED(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 18, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> WMISetLogoStatus(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 19, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> WMISetGamingKBBacklight(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 27, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> WMISetGamingRgbKbSetting(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 28, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> WMISetGamingLEDBehavior(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 29, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<uint> WMISetGamingMiscSetting(ulong intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			uint result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 33, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[9];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt32(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return uint.MaxValue;
		}
	}

	public static async Task<ulong> WMIGetGamingMiscSetting(uint intput)
	{
		try
		{
			NamedPipeClientStream cline_stream = new NamedPipeClientStream(".", "PredatorSense_service_namedpipe", PipeDirection.InOut);
			cline_stream.Connect();
			ulong result = await Task.Run(() =>
			{
				IPCMethods.SendCommandByNamedPipe(cline_stream, 34, intput);
				cline_stream.WaitForPipeDrain();
				byte[] array = new byte[13];
				cline_stream.Read(array, 0, array.Length);
				return BitConverter.ToUInt64(array, 5);
			}).ConfigureAwait(continueOnCapturedContext: false);
			cline_stream.Close();
			return result;
		}
		catch (Exception)
		{
			return 4294967295uL;
		}
	}
}
