﻿using System;
using System.Threading.Tasks;
using System.Xml.Serialization;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Generate random numbers
    /// </summary>
    [XmlRoot(ElementName = "Random", Namespace = "http://meep.example.com/Meep/V1")]
    public class Random : AMessageModule
    {
        [XmlAttribute]
        public int Min { get; set; }

        [XmlAttribute]
        public int Max { get; set; }

        private System.Random _rand = new System.Random();

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                if (Min != 0 || Max != 0)
                    return new Message
                    {
                        DerivedFrom = msg,
                        Value = _rand.Next(Min, Max)
                    };
                else
                    return new Message
                    {
                        DerivedFrom = msg,
                        Value = _rand.NextDouble()
                    };
            });
        }
    }
}
