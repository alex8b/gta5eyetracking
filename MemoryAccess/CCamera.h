#ifndef CCAMERA_H_
#define CCAMERA_H_
#pragma once
#include "include.h"

namespace MemoryAccess 
{
	public ref class CCamera abstract sealed
	{
	public:
		static System::UIntPtr GetCurrentCameraMatrix(System::UIntPtr baseAddress, int length);
	};
}
#endif
