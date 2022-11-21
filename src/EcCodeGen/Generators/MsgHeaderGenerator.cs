namespace EcCodeGen;

public class MsgHeaderGeneratorC
{
	private readonly Master _master;
	public MsgHeaderGeneratorC(Master master)
	{
		_master = master;
	}

	private const string snippet_0 = @"
#pragma once

#include <stdint.h>

typedef struct pdo_r2a_servo pdo_r2a_servo_t;
typedef struct pdo_a2r_servo pdo_a2r_servo_t;
typedef struct pdo_a2r       pdo_a2r_t;
typedef struct pdo_r2a       pdo_r2a_t;
typedef struct a2r           a2r_t;
typedef struct r2a           r2a_t;

#pragma pack(push, 1)
	";
	private const string snippet_1 = @"#pragma pack(pop)";

	// private string  



}