#region License
/* 
 * Copyright (C) 1999-2016 John K�ll�n.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Reko.Core;
using Reko.Core.Expressions;
using Reko.Analysis;
using Reko.UnitTests.Mocks;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace Reko.UnitTests.Analysis
{
	/// <summary>
	/// Tests to make sure DeadCodeElimination works.
	/// </summary>
	[TestFixture]
	public class DeadCodeTests : AnalysisTestBase
	{
		protected override void RunTest(Program program, TextWriter writer)
		{
			DataFlowAnalysis dfa = new DataFlowAnalysis(program, null,  new FakeDecompilerEventListener());
			dfa.UntangleProcedures();
			foreach (Procedure proc in program.Procedures.Values)
			{
                var sst = new SsaTransform2(
                    program.Architecture,
                    proc,
                    null,
                    dfa.ProgramDataFlow);
                sst.Transform();
				SsaState ssa = sst.SsaState;
				ConditionCodeEliminator cce = new ConditionCodeEliminator(ssa, program.Platform);
				cce.Transform();

				DeadCode.Eliminate(ssa);
				ssa.Write(writer);
				proc.Write(false, writer);
			}
		}

		[Test]
		public void DeadPushPop()
		{
			RunFileTest_x86_real("Fragments/pushpop.asm", "Analysis/DeadPushPop.txt");
		}

		[Test]
		public void DeadFactorialReg()
		{
			RunFileTest_x86_real("Fragments/factorial_reg.asm", "Analysis/DeadFactorialReg.txt");
		}

		[Test]
		public void DeadFactorial()
		{
			RunFileTest_x86_real("Fragments/factorial.asm", "Analysis/DeadFactorial.txt");
		}

		[Test]
		public void Dead3Converge()
		{
			RunFileTest_x86_real("Fragments/3converge.asm", "Analysis/Dead3Converge.txt");
		}

		[Test]
		public void DeadCmpMock()
		{
			RunFileTest(new CmpMock(), "Analysis/DeadCmpMock.txt");
		}

		[Test]
		public void DeadFnReturn()
		{
			ProcedureBuilder m = new ProcedureBuilder("foo");
			Identifier unused = m.Local32("unused");
			m.Assign(unused, m.Fn("foo", Constant.Word32(1)));
			m.Return();
			RunFileTest(m, "Analysis/DeadFnReturn.txt");
		}
 	}
}
