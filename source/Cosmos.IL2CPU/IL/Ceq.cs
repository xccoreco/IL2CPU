using System;
using XSharp;
using XSharp.Assembler;
using XSharp.Assembler.x86;
using static XSharp.Assembler.x86.SSE.ComparePseudoOpcodes;
using static XSharp.XSRegisters;

namespace Cosmos.IL2CPU.X86.IL
{
  [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Ceq)]
  public class Ceq : ILOp
  {
    public Ceq(XSharp.Assembler.Assembler aAsmblr)
      : base(aAsmblr)
    {
    }

    public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
    {
      var xStackItem = aOpCode.StackPopTypes[0];
      var xStackItemSize = SizeOfType(xStackItem);
      var xStackItemIsFloat = TypeIsFloat(xStackItem);
      var xStackItem2 = aOpCode.StackPopTypes[1];
      var xStackItem2Size = SizeOfType(xStackItem2);
      var xStackItem2IsFloat = TypeIsFloat(xStackItem2);
      var xSize = Math.Max(xStackItemSize, xStackItem2Size);

      var xNextLabel = GetLabel(aMethod, aOpCode.NextPosition);

      if (xSize > 8)
      {
        throw new Exception("Cosmos.IL2CPU.x86->IL->Ceq.cs->Error: StackSizes > 8 not supported");
      }
      else if (xSize <= 4)
      {
        if (xStackItemIsFloat) // float
        {
          XS.SSE.MoveSS(XMM0, ESP, sourceIsIndirect: true);
          XS.Add(ESP, 4);
          XS.SSE.MoveSS(XMM1, ESP, sourceIsIndirect: true);
          XS.SSE.CompareSS(XMM1, XMM0, comparision: Equal);
          XS.MoveD(EBX, XMM1);
          XS.And(EBX, 1);
          XS.Set(ESP, EBX, destinationIsIndirect: true);
        }
        else
        {
          XS.Pop(EAX);
          XS.Compare(EAX, ESP, sourceIsIndirect: true);
          XS.Jump(ConditionalTestEnum.Equal, Label.LastFullLabel + ".True");
          XS.Jump(Label.LastFullLabel + ".False");
          XS.Label(".True");
          XS.Add(ESP, 4);
          XS.Push(1);
          XS.Jump(xNextLabel);
          XS.Label(".False");
          XS.Add(ESP, 4);
          XS.Push(0);
          XS.Jump(xNextLabel);
        }
      }
      else if (xSize > 4)
      {
        if (xStackItemIsFloat)
        {
          // Please note that SSE supports double operations only from version 2
          XS.SSE2.MoveSD(XMM0, ESP, sourceIsIndirect: true);
          // Increment ESP to get the value of the next double
          XS.Add(ESP, 8);
          XS.SSE2.MoveSD(XMM1, ESP, sourceIsIndirect: true);
          XS.SSE2.CompareSD(XMM1, XMM0, comparision: Equal);
          XS.MoveD(EBX, XMM1);
          XS.And(EBX, 1);
          // We need to move the stack pointer of 4 Byte to "eat" the second double that is yet in the stack or we get a corrupted stack!
          XS.Add(ESP, 4);
          XS.Set(ESP, EBX, destinationIsIndirect: true);
        }
        else
        {
          if (IsReferenceType(xStackItem) && IsReferenceType(xStackItem2))
          {
            XS.Comment(xStackItem.Name);
            XS.Add(ESP, 4);
            XS.Pop(EAX);

            XS.Comment(xStackItem2.Name);
            XS.Add(ESP, 4);
            XS.Pop(EBX);

            XS.Compare(EAX, EBX);
            XS.Jump(ConditionalTestEnum.NotEqual, Label.LastFullLabel + ".False");

            // equal
            XS.Push(1);
            XS.Jump(xNextLabel);
            XS.Label(Label.LastFullLabel + ".False");
            //not equal
            XS.Push(0);
            XS.Jump(xNextLabel);
          }
          else
          {
            XS.Pop(EAX);
            XS.Compare(EAX, ESP, sourceDisplacement: 4);
            XS.Pop(EAX);
            XS.Jump(ConditionalTestEnum.NotEqual, Label.LastFullLabel + ".False");
            XS.Xor(EAX, ESP, sourceDisplacement: 4);
            XS.Jump(ConditionalTestEnum.NotZero, Label.LastFullLabel + ".False");

            //they are equal
            XS.Add(ESP, 8);
            XS.Push(1);
            XS.Jump(xNextLabel);
            XS.Label(Label.LastFullLabel + ".False");
            //not equal
            XS.Add(ESP, 8);
            XS.Push(0);
            XS.Jump(xNextLabel);

          }
        }
      }
      else
      {
        throw new Exception("Cosmos.IL2CPU.x86->IL->Ceq.cs->Error: Case not handled!");
      }
    }
  }
}
