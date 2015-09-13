#include "CCamera.h"

namespace MemoryAccess
{
#pragma managed(push, off)
	CViewPortGame* GetViewPortGame(DWORD64 baseAddress, DWORD64 length)
	{
		if (g_pViewPortGame == NULL) {
			DWORD64 matricesManagerInc = Pattern::Scan(baseAddress, length, "48 8B 15 ?? ?? ?? ?? 48 8D 2D ?? ?? ?? ?? 48 8B CD"); // Matrices manager instruction
			g_pViewPortGame = *reinterpret_cast<CViewPortGame**>(*reinterpret_cast<DWORD*>(matricesManagerInc + 3) + matricesManagerInc + 7);
		}
		return g_pViewPortGame;
	}
#pragma managed(pop)
	SharpDX::Matrix CCamera::GetCurrentCameraMatrix(System::IntPtr baseAddress, int length)
	{
		//Log::Init(false);
		auto viewPortGame = GetViewPortGame(baseAddress.ToInt64(), length);
		
		if (viewPortGame != NULL)
		{
			auto matrix = viewPortGame->mViewMatrix;
			return Matrix(matrix[0], matrix[1], matrix[2], matrix[3], matrix[4], matrix[5], matrix[6], matrix[7], matrix[8], matrix[9], matrix[10], matrix[11], matrix[12], matrix[13], matrix[14], matrix[15]);
		}
		else
		{
			return Matrix().Zero;
		}			
	}

	DWORD64 CCamera::GetPointer() {
		return (DWORD64)g_pViewPortGame;
	}
}