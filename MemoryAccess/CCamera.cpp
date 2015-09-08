#include "CCamera.h"

namespace MemoryAccess
{
	DWORD64 g_unknownInt,
		g_currentCamera;
	DWORD *g_tlsValue;

#pragma managed(push, off)
	DWORD64 GetCurrentCamera(DWORD64 baseAddress, DWORD64 length)
	{
		DWORD64 unknownInt;
		if (g_currentCamera == NULL || g_unknownInt == NULL || g_tlsValue == NULL)
		{
			DWORD64 g_TlsIndexopCode = NULL,
				tlsValue,
				*TLSArray = (DWORD64*)__readgsqword(0x58),
				tlsIndex;

			while (g_TlsIndexopCode == NULL || g_unknownInt == NULL || g_currentCamera == NULL)
			{
				if (g_TlsIndexopCode == NULL)
					g_TlsIndexopCode = Pattern::Scan(baseAddress, length, "8B 05 ? ? ? ? F3 0F 10 05 ? ? ? ? 4D 8B 14 C1");
				if (g_unknownInt == NULL)
					g_unknownInt = Pattern::Scan(baseAddress, length, "8B 05 ? ? ? ? 0F 44 05 ? ? ? ? 48 83 64 24 ? ?");
				if (g_currentCamera == NULL)
					g_currentCamera = Pattern::Scan(baseAddress, length, "48 8D 05 ? ? ? ? 48 69 F6 ? ? ? ? 48 03 F0 48 8B CE");
			}

			if (g_TlsIndexopCode != NULL) // extra safe
				tlsIndex = *(DWORD*)(g_TlsIndexopCode + 2) + g_TlsIndexopCode + 6;
			else
				return 0;

			//Log::Write(Log::Type::Debug, "TLS array pointer: %I64X and tlsIndex: %I64X", TLSArray, tlsIndex);

			if (tlsIndex != NULL && TLSArray != NULL) // extra safeTlsIndexopCode
				tlsValue = TLSArray[(DWORD64)*(DWORD*)tlsIndex];
			else
				return 0;

			//delete TLSArray;

			//Log::Write(Log::Type::Debug, "TLS value pointer: %I64X", tlsValue);

			if (tlsValue != NULL) // extra safe
				g_tlsValue = (DWORD*)(tlsValue + 0xB4);

			//Log::Write(Log::Type::Debug, "TLS second value: %d", tlsValue);

			if (!(*g_tlsValue >> 1 & 1))
				g_unknownInt += 6;

			//Log::Write(Log::Type::Debug, "Unknown Int: %I64X", g_unknownInt);

			if (g_currentCamera != NULL && g_unknownInt != NULL) // extra safe
			{
				//Log::Write(Log::Type::Debug, "Current camera(OPCODE): %I64X", g_currentCamera);

				g_currentCamera = *(DWORD*)(g_currentCamera + 3) + g_currentCamera + 7;

				//Log::Write(Log::Type::Debug, "Current camera: %I64X", g_currentCamera);

				if ((*(UINT8*)g_unknownInt) == 0x8B)
					unknownInt = *(DWORD*)(g_unknownInt + 2) + g_unknownInt + 6;
				else
					unknownInt = *(DWORD*)(g_unknownInt + 3) + g_unknownInt + 7;
				g_unknownInt -= 6;

				//Log::Write(Log::Type::Debug, "Unknown Int: %I64X", g_unknownInt);

				return g_currentCamera + 0x490 * *(DWORD*)unknownInt;
			}
			else
				return 0;
		}
		else
		{
			if (!(*g_tlsValue >> 1 & 1))
				g_unknownInt += 6;

			if ((*(UINT8*)g_unknownInt) == 0x8B)
				unknownInt = *(DWORD*)(g_unknownInt + 2) + g_unknownInt + 6;
			else
				unknownInt = *(DWORD*)(g_unknownInt + 3) + g_unknownInt + 7;
			g_unknownInt -= 6;

			return g_currentCamera + 0x490 * *(DWORD*)unknownInt;
		}
	}
#pragma managed(pop)
	SharpDX::Matrix CCamera::GetCurrentCameraMatrix(System::IntPtr baseAddress, int length)
	{
		//Log::Init(false);
		float* matrix = (float*)GetCurrentCamera(baseAddress.ToInt64(), length) + 184;
		if (matrix != NULL)
			return Matrix(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], matrix[6], matrix[7], matrix[8], matrix[9], matrix[10], matrix[11], matrix[12], matrix[13], matrix[14], matrix[15]);
		else
			return Matrix().Zero;
	}
}