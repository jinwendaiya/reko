// test.c
// Generated by decompiling test
// using Reko decompiler version 0.8.0.0.

#include "test.h"

// 00000588: void _init()
void _init()
{
	<anonymous> * r2_11 = globals->ptr10AE0;
	if (r2_11 == null)
	{
		word32 sp_24;
		word32 r28_25;
		word32 r25_26;
		word32 ra_27;
		word32 r2_28;
		(*(ptr32 *) 0x00020A4C + 2004)();
		word32 sp_35;
		word32 r28_36;
		word32 r25_37;
		word32 ra_38;
		word32 r2_39;
		(*(ptr32 *) 0x00020A4C + 0x0970)();
	}
	else
	{
		<anonymous> * r25_12 = globals->ptr10AE0;
		word32 sp_13;
		word32 r28_14;
		word32 r25_15;
		word32 ra_16;
		word32 r2_17;
		r25_12();
	}
}

// 00000610: void __start(Stack word32 dwArg00)
void __start(word32 dwArg00)
{
	<anonymous> * r25_23 = *(<anonymous> **) 0x00020A24;
	word32 sp_24;
	word32 ra_25;
	word32 r0_26;
	word32 r28_27;
	word32 r4_28;
	word32 r5_29;
	word32 r6_30;
	word32 r1_31;
	word32 r7_32;
	word32 r8_33;
	word32 r2_34;
	word32 r25_35;
	r25_23();
	while (true)
		;
}

// 00000670: void deregister_tm_clones()
void deregister_tm_clones()
{
	word32 r2_8 = globals->dw10AA4;
	if (r2_8 == globals->ptr10AA8 + 2692)
		return;
	<anonymous> * r25_18 = globals->ptr10AE4;
	if (r25_18 == null)
		return;
	word32 sp_19;
	word32 r28_20;
	word32 r25_21;
	word32 r4_22;
	word32 r2_23;
	word32 ra_24;
	r25_18();
}

// 000006A8: void register_tm_clones()
void register_tm_clones()
{
	int32 r5_10 = globals->dw10AA4 - (globals->ptr10AA8 + 2692);
	Eq_82 r2_12 = r5_10 >> 0x02 >> 0x1F;
	if (r2_12 + (r5_10 >> 0x02) >> 0x01 == 0x00)
		return;
	<anonymous> * r25_22 = globals->ptr10AD4;
	if (r25_22 == null)
		return;
	word32 sp_23;
	word32 r28_24;
	word32 r25_25;
	word32 r4_26;
	word32 r5_27;
	word32 r2_28;
	r25_22();
}

// 000006F4: void __do_global_dtors_aux()
void __do_global_dtors_aux()
{
	struct Eq_59 * r19_10 = globals->ptr10AA8;
	if ((word32) r19_10[2800] == 0x00)
	{
		<anonymous> * r2_32 = globals->ptr10AE8;
		if (r2_32 != null)
		{
			<anonymous> * r25_89 = globals->ptr10AE8;
			word32 sp_92;
			word32 r28_93;
			word32 r25_94;
			word32 ra_96;
			word32 r18_97;
			word32 r17_98;
			word32 r16_99;
			word32 r2_100;
			word32 r4_101;
			word32 r3_102;
			r25_89();
		}
		struct Eq_59 * r17_41 = globals->ptr10AA8;
		struct Eq_59 * r2_39 = globals->ptr10AA8;
		Eq_124 r2_45 = r17_41[0x0AF4];
		Eq_124 r16_47 = (globals->dw10AB0 - (r2_39 + 2664) >> 0x02) + -0x01;
		word32 r18_43 = r2_39 + 2664;
		if ((word32) (r2_45 < r16_47) != 0x00)
		{
			do
			{
				uint32 r2_66 = (word32) r2_45.u1 + 0x01;
				r17_41[0x0AF4] = (struct Eq_59) r2_66;
				<anonymous> ** r2_69 = r18_43 + (r2_66 << 0x02);
				<anonymous> * r25_70 = *r2_69;
				word32 sp_71;
				word32 r28_72;
				word32 r25_73;
				word32 ra_75;
				Eq_124 r16_78;
				word32 r2_79;
				word32 r4_80;
				word32 r3_81;
				r25_70();
				r2_45 = r17_41[0x0AF4];
			} while ((word32) (r2_45 < r16_78) != 0x00);
		}
		deregister_tm_clones();
		r19_10[2800] = (struct Eq_59) 0x01;
	}
}

// 000007D4: void frame_dummy()
void frame_dummy()
{
	register_tm_clones();
}

// 000007F0: void main(Stack word32 dwArg00, Stack word32 dwArg04)
void main(word32 dwArg00, word32 dwArg04)
{
	memset(r4_20, r5_19, r6_18);
	calloc(r4_35, r5_34);
	r2_45->t0000 = dwLoc14;
	r2_45->b0004 = (byte) (word32) bLoc10;
	r2_45->t0000.u0 = 0x0C;
	r2_45->dw0001 = 0x00;
	struct Eq_216 * r2_50 = r2_45;
	Eq_215 r3_52 = dwLoc14;
	struct Eq_216 * r2_62 = r2_45;
}

// 000008C0: void __libc_csu_init()
void __libc_csu_init()
{
	_init();
	word32 * r16_31 = globals->ptr10ABC;
	if (globals->ptr10ABC - r16_31 >> 0x02 != 0x00)
	{
		while (true)
		{
			<anonymous> * r25_54 = *r16_31;
			word32 sp_59;
			word32 r28_60;
			word32 r25_61;
			word32 r21_62;
			word32 r6_63;
			word32 r20_64;
			word32 r5_65;
			word32 r19_66;
			word32 r4_67;
			word32 r18_68;
			word32 r16_69;
			word32 ra_70;
			word32 r17_71;
			r25_54();
			if (r18_68 == r17_71)
				break;
			r16_31 = r16_69 + 0x04;
		}
	}
}

// 00000964: void __libc_csu_fini()
void __libc_csu_fini()
{
}

// 00000970: void __do_global_ctors_aux()
void __do_global_ctors_aux()
{
	struct Eq_59 * r3_7 = globals->ptr10AA8;
	<anonymous> * r25_17 = r3_7[2656];
	if (r25_17 != (<anonymous> *) -0x01)
	{
		do
		{
			word32 sp_30;
			word32 r28_31;
			word32 r25_32;
			word32 r3_33;
			word32 r2_34;
			word32 ra_35;
			word32 r17_36;
			int32 * r16_37;
			r25_17();
		} while (*r16_37 != -0x01);
	}
}

// 000009D0: void __libc_start_main(Register ptr32 r28)
void __libc_start_main(ptr32 r28)
{
	<anonymous> * r25_4 = *(r28 - 0x7FF0);
	word32 sp_8;
	word32 r28_9;
	word32 r25_10;
	word32 ra_11;
	word32 r15_12;
	word32 r24_13;
	r25_4();
}

// 000009E0: void memset(Register ptr32 r28)
void memset(ptr32 r28)
{
	<anonymous> * r25_4 = *(r28 - 0x7FF0);
	word32 sp_8;
	word32 r28_9;
	word32 r25_10;
	word32 ra_11;
	word32 r15_12;
	word32 r24_13;
	r25_4();
}

// 000009F0: void calloc(Register ptr32 r28)
void calloc(ptr32 r28)
{
	<anonymous> * r25_4 = *(r28 - 0x7FF0);
	word32 sp_8;
	word32 r28_9;
	word32 r25_10;
	word32 ra_11;
	word32 r15_12;
	word32 r24_13;
	r25_4();
	_fini();
}

// 00000A10: void _fini()
void _fini()
{
	word32 sp_17;
	word32 r28_18;
	word32 r25_19;
	word32 ra_20;
	(*(ptr32 *) 0x00020A4C + 0x06F4)();
}

