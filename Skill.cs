// <copyright file="Skill.cs" company="Windower Team">
// Copyright © 2013-2014 Windower Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
// </copyright>

namespace ResourceExtractor
{
    internal enum Skill
    {
        None                = 0x00,

        HandToHand          = 0x01,
        Dagger              = 0x02,
        Sword               = 0x03,
        GreatSword          = 0x04,
        Axe                 = 0x05,
        GreatAxe            = 0x06,
        Scythe              = 0x07,
        Polearm             = 0x08,
        Katana              = 0x09,
        GreatKatana         = 0x0A,
        Club                = 0x0B,
        Staff               = 0x0C,

        AutomatonMelee      = 0x16,
        AutomatonArchery    = 0x17,
        AutomatonMagic      = 0x18,

        Archery             = 0x19,
        Marksmanship        = 0x1A,
        Throwing            = 0x1B,

        Guard               = 0x1C,
        Evasion             = 0x1D,
        Shield              = 0x1E,
        Parrying            = 0x1F,

        DivineMagic         = 0x20,
        HealingMagic        = 0x21,
        EnhancingMagic      = 0x22,
        EnfeeblingMagic     = 0x23,
        ElementalMagic      = 0x24,
        DarkMagic           = 0x25,
        SummoningMagic      = 0x26,
        Ninjutsu            = 0x27,
        Singing             = 0x28,
        StringedInstrument  = 0x29,
        WindInstrument      = 0x2A,
        BlueMagic           = 0x2B,
        Geomancy            = 0x2C,
        Handbell            = 0x2D,

        Fishing             = 0x30,
        Woodworking         = 0x31,
        Smithing            = 0x32,
        Goldsmithing        = 0x33,
        Clothcraft          = 0x34,
        Leathercraft        = 0x35,
        Bonecraft           = 0x36,
        Alchemy             = 0x37,
        Cooking             = 0x38,
        Synergy             = 0x39,
    }
}
