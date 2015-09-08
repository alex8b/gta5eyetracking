#ifndef CCAMERA_H_
#define CCAMERA_H_
#pragma once
#include "include.h"
#using <SharpDX.dll>

using namespace SharpDX;

namespace MemoryAccess
{
	public ref class CCamera abstract sealed
	{
	public:
		static Matrix GetCurrentCameraMatrix(System::IntPtr baseAddress, int length);
	};
}
#endif