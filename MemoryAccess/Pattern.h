#pragma once
#include "include.h"

//Copyright (c) 2015 s0beit
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions :
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

struct PatternByte
{
	PatternByte() : ignore(true) {
		//
	}

	PatternByte(std::string byteString, bool ignoreThis = false) {
		data = StringToUint8(byteString);
		ignore = ignoreThis;
	}

	bool ignore;
	UINT8 data;

private:
	UINT8 StringToUint8(std::string str) {
		std::istringstream iss(str);

		UINT32 ret;

		if (iss >> std::hex >> ret) {
			return (UINT8)ret;
		}

		return 0;
	}
};

struct Pattern
{
	static DWORD64 Scan(DWORD64 dwStart, DWORD64 dwLength, std::string s) {
		std::vector<PatternByte> p;
		std::istringstream iss(s);
		std::string w;

		while (iss >> w) {
			if (w.data()[0] == '?') { // Wildcard
				p.push_back(PatternByte());
			}
			else if (w.length() == 2 && isxdigit(w.data()[0]) && isxdigit(w.data()[1])) { // Hex
				p.push_back(PatternByte(w));
			}
			else {
				return NULL; // You dun fucked up
			}
		}

		for (DWORD64 i = 0; i < dwLength; i++) {
			UINT8* lpCurrentByte = (UINT8*)(dwStart + i);

			bool found = true;

			for (size_t ps = 0; ps < p.size(); ps++) {
				if (p[ps].ignore == false && lpCurrentByte[ps] != p[ps].data) {
					found = false;
					break;
				}
			}

			if (found) {
				return (DWORD64)lpCurrentByte;
			}
		}

		return NULL;
	}

	static DWORD64 Scan(MODULEINFO mi, std::string s) {
		return Scan((DWORD64)mi.lpBaseOfDll, (DWORD64)mi.SizeOfImage, s);
	}
};