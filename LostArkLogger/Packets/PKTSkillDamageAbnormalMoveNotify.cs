﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostArkLogger
{
    public class PKTSkillDamageAbnormalMoveNotify
    {
        // FC-8F-0F-00-01-C1-35-05-21-00-00-00-00-66-8E-01-00-01-00-E1-89-05-21-00-00-00-00-A2-02-E4-49-0A-84-4C-0A-00-00-00-00-00
        public UInt32 SkillIdWithState; // 360003
        public UInt32 SkillId; // 36000
        public UInt16 NumEvents; // 0x0001
        public List<PKTSkillDamageNotify.SkillDamageNotifyEvent> Events;
        public UInt64 PlayerId;
        public PKTSkillDamageAbnormalMoveNotify(Byte[] Bytes)
        {
            // need to fix still
            var bitReader = new BitReader(Bytes);
            PlayerId = bitReader.ReadUInt64();
            NumEvents = bitReader.ReadUInt16();
            Events = new List<PKTSkillDamageNotify.SkillDamageNotifyEvent>();
            for (var i = 0; i < NumEvents; i++)
            {
                var dmgEvent = new PKTSkillDamageNotify.SkillDamageNotifyEvent();
                dmgEvent.TargetId = bitReader.ReadUInt64();
                dmgEvent.DamageBits = (UInt16)bitReader.ReadBits(4);
                dmgEvent.Damage = bitReader.ReadBits(4 + dmgEvent.DamageBits * 4);
                var a = (UInt16)bitReader.ReadBits(1);
                var b = (UInt16)(bitReader.ReadBits(3) << 1);
                var c = bitReader.ReadBits(4 + b * 4);
                var d = (UInt16)bitReader.ReadBits(4);
                bitReader.ReadBits(4 + d * 4);
                dmgEvent.FlagsMaybe = bitReader.ReadUInt16();
                dmgEvent.Unk3 = (UInt32)bitReader.ReadUInt16();
                var extra = bitReader.ReadByte();
                for (var j = 0; j < extra; j++) bitReader.ReadByte();
                Events.Add(dmgEvent);
            }
            SkillIdWithState = bitReader.ReadUInt32();
            SkillId = bitReader.ReadUInt32();
            bitReader.ReadByte();
        }
    }
}
