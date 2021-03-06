﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core
{
    public sealed class TrainerCard8 : SaveBlock
    {
        public TrainerCard8(SAV8SWSH sav, SCBlock block) : base (sav, block.Data) { }

        public string OT
        {
            get => SAV.GetString(Data, 0x00, 0x1A);
            set => SAV.SetData(Data, SAV.SetString(value, SAV.OTLength), 0x00);
        }

        public int TrainerID
        {
            get => BitConverter.ToInt32(Data, 0x1C);
            set => SAV.SetData(Data, BitConverter.GetBytes(value), 0x1C);
        }

        public const int RotoRallyScoreMax = 99_999;

        public int RotoRallyScore
        {
            get => BitConverter.ToInt32(Data, 0x28);
            set
            {
                if (value > RotoRallyScoreMax)
                    value = RotoRallyScoreMax;
                var data = BitConverter.GetBytes(value);
                SAV.SetData(Data, data, 0x28);
                // set to the other block since it doesn't have an accessor
                ((SAV8SWSH)SAV).SetValue(SaveBlockAccessorSWSH.KRotoRally, (uint)value);
            }
        }

        public string Number
        {
            get => Encoding.ASCII.GetString(Data, 0x39, 3);
            set
            {
                for (int i = 0; i < 3; i++)
                    Data[0x39 + i] = (byte) (value.Length > i ? value[i] : '\0');
                SAV.Edited = true;
            }
        }

        // Trainer Card Pokemon
        // 0xC8 - 0xE3 (0x1C)
        // 0xE4
        // 0x100
        // 0x11C
        // 0x138
        // 0x154 - 0x16F

        /// <summary>
        /// Gets an object that exposes the data of the corresponding party <see cref="index"/>.
        /// </summary>
        public TrainerCard8Poke ViewPoke(int index)
        {
            if ((uint) index >= 6)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new TrainerCard8Poke(Data, Offset + 0xC8 + (index * TrainerCard8Poke.SIZE));
        }

        /// <summary>
        /// Applies the current <see cref="SaveFile.PartyData"/> to the block.
        /// </summary>
        public void SetPartyData() => LoadTeamData(SAV.PartyData);

        public void LoadTeamData(IList<PKM> party)
        {
            for (int i = 0; i < party.Count; i++)
                ViewPoke(i).LoadFrom(party[i]);
        }
    }

    public class TrainerCard8Poke
    {
        public const int SIZE = 0x1C;
        private readonly byte[] Data;
        private readonly int Offset;

        public TrainerCard8Poke(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }

        public int Species
        {
            get => BitConverter.ToInt32(Data, Offset + 0x00);
            set => BitConverter.GetBytes(value).CopyTo(Data, Offset + 0x00);
        }

        public int AltForm
        {
            get => BitConverter.ToInt32(Data, Offset + 0x04);
            set => BitConverter.GetBytes(value).CopyTo(Data, Offset + 0x04);
        }

        public int Gender
        {
            get => BitConverter.ToInt32(Data, Offset + 0x08);
            set => BitConverter.GetBytes(value).CopyTo(Data, Offset + 0x08);
        }

        public bool IsShiny
        {
            get => Data[Offset + 0xC] != 0;
            set => Data[Offset + 0xC] = (byte) (value ? 1 : 0);
        }

        public uint EncryptionConstant
        {
            get => BitConverter.ToUInt32(Data, Offset + 0x10);
            set => BitConverter.GetBytes(value).CopyTo(Data, Offset + 0x10);
        }

        public uint Unknown
        {
            get => BitConverter.ToUInt32(Data, Offset + 0x14);
            set => BitConverter.GetBytes(value).CopyTo(Data, Offset + 0x14);
        }

        public int FormArgument
        {
            get => BitConverter.ToInt32(Data, Offset + 0x18);
            set => BitConverter.GetBytes(value).CopyTo(Data, Offset + 0x18);
        }

        public void LoadFrom(PKM pkm)
        {
            Species = pkm.Species;
            AltForm = pkm.AltForm;
            Gender = pkm.Gender;
            IsShiny = pkm.IsShiny;
            EncryptionConstant = pkm.EncryptionConstant;
            FormArgument = pkm is IFormArgument f && pkm.Species == (int) Core.Species.Alcremie ? (int)f.FormArgument : -1;
        }

        public void LoadFrom(TitleScreen8Poke pkm)
        {
            Species = pkm.Species;
            AltForm = pkm.AltForm;
            Gender = pkm.Gender;
            IsShiny = pkm.IsShiny;
            EncryptionConstant = pkm.EncryptionConstant;
            FormArgument = pkm.FormArgument;
        }
    }
}