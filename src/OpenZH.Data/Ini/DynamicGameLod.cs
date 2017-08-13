﻿using System;
using OpenZH.Data.Ini.Parser;

namespace OpenZH.Data.Ini
{
    public sealed class DynamicGameLod
    {
        internal static DynamicGameLod Parse(IniParser parser)
        {
            return parser.ParseTopLevelNamedBlock(
                (x, name) => x.Level = (DynamicGameLodLevel) Enum.Parse(typeof(DynamicGameLodLevel), name),
                FieldParseTable);
        }

        private static readonly IniParseTable<DynamicGameLod> FieldParseTable = new IniParseTable<DynamicGameLod>
        {
            { "MinimumFPS", (parser, x) => x.MinimumFps = parser.ParseInteger() },
            { "ParticleSkipMask", (parser, x) => x.ParticleSkipMask = parser.ParseInteger() },
            { "DebrisSkipMask", (parser, x) => x.DebrisSkipMask = parser.ParseInteger() },
            { "SlowDeathScale", (parser, x) => x.SlowDeathScale = parser.ParseFloat() },
            { "MinParticlePriority", (parser, x) => x.MinParticlePriority = parser.ParseEnum<ParticleSystemPriority>() },
            { "MinParticleSkipPriority", (parser, x) => x.MinParticleSkipPriority = parser.ParseEnum<ParticleSystemPriority>() },
        };

        public DynamicGameLodLevel Level { get; private set; }

        public int MinimumFps { get; private set; }
        public int ParticleSkipMask { get; private set; }
        public int DebrisSkipMask { get; private set; }
        public float SlowDeathScale { get; private set; }
        public ParticleSystemPriority MinParticlePriority { get; private set; }
        public ParticleSystemPriority MinParticleSkipPriority { get; private set; }
    }

    public enum DynamicGameLodLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum ParticleSystemPriority
    {
        [IniEnum("WEAPON_EXPLOSION")]
        WeaponExplosion,

        [IniEnum("SCORCHMARK")]
        ScorchMark,

        [IniEnum("DUST_TRAIL")]
        DustTrail,

        [IniEnum("BUILDUP")]
        Buildup,

        [IniEnum("DEBRIS_TRAIL")]
        DebrisTrail,

        [IniEnum("UNIT_DAMAGE_FX")]
        UnitDamageFX,

        [IniEnum("DEATH_EXPLOSION")]
        DeathExplosion,

        [IniEnum("SEMI_CONSTANT")]
        SemiConstant,

        [IniEnum("CONSTANT")]
        Constant,

        [IniEnum("WEAPON_TRAIL")]
        WeaponTrail,

        [IniEnum("AREA_EFFECT")]
        AreaEffect,

        [IniEnum("CRITICAL")]
        Critical,

        [IniEnum("ALWAYS_RENDER")]
        AlwaysRender
    }
}
