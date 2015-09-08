#include "CCamera.h"
namespace MemoryAccess
{
#pragma managed(push, off)
	DWORD64 GetCurrentCamera(DWORD64 baseAddress, DWORD64 length)
	{
		DWORD64 TlsIndexopCode = Pattern::Scan(baseAddress, length, "8B 05 ? ? ? ? F3 0F 10 05 ? ? ? ? 4D 8B 14 C1"),
			unknownInt = Pattern::Scan(baseAddress, length, "8B 05 ? ? ? ? 0F 44 05 ? ? ? ? 48 83 64 24 ? ?"),
			currentCamera = Pattern::Scan(baseAddress, length, "48 8D 05 ? ? ? ? 48 69 F6 ? ? ? ? 48 03 F0 48 8B CE"),
			tlsValue;

		DWORD64* TLSArray = (DWORD64*)__readgsqword(0x58);
		DWORD64* tlsIndex = (DWORD64*)(*(DWORD64*)(TlsIndexopCode + 2) + TlsIndexopCode + 6);


		tlsValue = TLSArray[*tlsIndex];
		tlsValue = (*(DWORD64*)tlsValue + 0xB4);
		if (!(tlsValue >> 1 & 1))
			unknownInt += 6;

		currentCamera = (*(DWORD64*)(currentCamera + 3) + currentCamera + 7);
		if (((UINT8)unknownInt) == 0x8B)
			unknownInt = (*(DWORD64*)(unknownInt + 2) + unknownInt + 6);
		else
			unknownInt = (*(DWORD64*)(unknownInt + 3) + unknownInt + 7);

		return currentCamera = currentCamera + 0x490 * *((DWORD64*)unknownInt);
	}
#pragma managed(pop)
	System::UIntPtr CCamera::GetCurrentCameraMatrix(System::UIntPtr baseAddress, int length)
	{		
		return System::UIntPtr(GetCurrentCamera(baseAddress.ToUInt64(), length) + 184);
	}
}