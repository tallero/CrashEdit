using System;
using System.Collections.Generic;
using System.Reflection;

namespace Crash
{
    public sealed class GOOLEntry : Entry
    {
        internal static Dictionary<GOOLVersion,Dictionary<int,Type>> opsets;

        static GOOLEntry()
        {
            var assembly_types = Assembly.GetExecutingAssembly().GetTypes();
            opsets = new Dictionary<GOOLVersion,Dictionary<int,Type>>();
            foreach (Type type in assembly_types)
            {
                foreach (GOOLInstructionAttribute attribute in type.GetCustomAttributes(typeof(GOOLInstructionAttribute), false))
                {
                    if (!opsets.ContainsKey(attribute.Version))
                    {
                        opsets.Add(attribute.Version,new Dictionary<int,Type>());
                    }
                    Dictionary<int,Type> opset = opsets[attribute.Version];
                    if (!opset.ContainsKey(attribute.Opcode))
                    {
                        opset.Add(attribute.Opcode,type);
                    }
                }
            }
        }

        internal GOOLInstruction LoadInstruction(int ins, bool mips)
        {
            if (!mips)
            {
                if (opsets.ContainsKey(Version))
                {
                    Dictionary<int, Type> opset = opsets[Version];
                    int opcode = ins >> 24 & 0xFF;
                    if (opset.ContainsKey(opcode))
                    {
                        try
                        {
                            return (GOOLInstruction)Activator.CreateInstance(opset[opcode],ins,this);
                        }
                        catch (TargetInvocationException ex)
                        {
                            throw ex.InnerException;
                        }
                    }
                    else
                        return new GOOLInvalidInstruction(ins, this);
                }
                else
                    return new GOOLInvalidInstruction(ins, this);
            }
            else
            {
                return new MIPSInstruction(ins,this);
            }
        }

        private List<GOOLInstruction> instructions;
        private List<GOOLStateDescriptor> statedescriptors;
        //private List<GOOLAnimation> anims;

        public GOOLEntry(GOOLVersion version,byte[] header,byte[] instructions,int[] data,short[] statemap,IEnumerable<GOOLStateDescriptor> statedescriptors,byte[] anims,int eid) : base(eid)
        {
            Version = version;
            Header = header;
            this.instructions = new List<GOOLInstruction>();
            bool mips = false;
            for (int i = 0; i < instructions.Length / 4; ++i)
            {
                GOOLInstruction ins = LoadInstruction(BitConv.FromInt32(instructions,i*4),mips);
                this.instructions.Add(ins);
                if (mips)
                {
                    MIPSInstruction prev = null;
                    if (this.instructions[this.instructions.Count-2] is MIPSInstruction)
                        prev = (MIPSInstruction)this.instructions[this.instructions.Count-2];
                    if (prev != null && (prev.Value == 0x03E0A809 || prev.Value == 0x03E00008)) // native mips returns or ends here
                    {
                        mips = false;
                    }
                }
                else
                    mips = GOOLInterpreter.IsMIPSInstruction(ins);
            }
            Data = data;
            StateMap = statemap;
            if (statedescriptors == null)
                this.statedescriptors = null;
            else
                this.statedescriptors = new List<GOOLStateDescriptor>(statedescriptors);
            Anims = anims;
        }

        public override int Type => 11;

        public GOOLVersion Version { get; }

        public byte[] Header { get; }
        public int[] Data { get; }
        public short[] StateMap { get; }
        public IList<GOOLStateDescriptor> StateDescriptors => statedescriptors;
        public byte[] Anims;

        public int Format => BitConv.FromInt32(Header,8);

        public IList<GOOLInstruction> Instructions => instructions;

        public override UnprocessedEntry Unprocess()
        {
            int itemcount =
                Anims != null ? 6 : (
                statedescriptors != null ? 5 : (
                StateMap != null ? 4 : 3
                )
                );
                
            byte[][] items = new byte [itemcount][];
            items[0] = Header;
            items[1] = new byte [instructions.Count * 4];
            for (int i = 0;i < instructions.Count;++i)
            {
                BitConv.ToInt32(items[1],i*4,instructions[i].Save());
            }
            items[2] = new byte[Data.Length*4];
            for (int i = 0; i < Data.Length; ++i)
            {
                BitConv.ToInt32(items[2],i*4,Data[i]);
            }
            if (itemcount > 3)
            {
                items[3] = new byte[StateMap.Length*2];
                for (int i = 0; i < StateMap.Length;++i)
                {
                    BitConv.ToInt16(items[3],i*2,StateMap[i]);
                }
                if (itemcount > 4)
                {
                    items[4] = new byte[statedescriptors.Count*0x10];
                    for (int i = 0; i < statedescriptors.Count; ++i)
                    {
                        statedescriptors[i].Save().CopyTo(items[4],i*0x10);
                    }
                    if (itemcount > 5)
                    {
                        items[5] = Anims;
                    }
                }
            }
            return new UnprocessedEntry(items,EID,Type);
        }
    }
}
