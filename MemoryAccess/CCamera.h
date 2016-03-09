#ifndef CCAMERA_H_
#define CCAMERA_H_
#pragma once
#include "include.h"
#using <SharpDX.dll>
#using <SharpDX.Mathematics.dll>

using namespace SharpDX;

namespace MemoryAccess
{
	public ref class CCamera abstract sealed
	{
	public:
		static Matrix GetCurrentCameraMatrix(System::IntPtr baseAddress, int length);
		static DWORD64 GetPointer();
	};

	class CViewPortGame
	{
	public:
		char _0x0000[588];
		float mViewMatrix[16]; //0x024C 

	};//Size=0x028C

	CViewPortGame* g_pViewPortGame;
}
#endif