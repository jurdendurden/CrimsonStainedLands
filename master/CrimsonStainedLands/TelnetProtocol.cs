﻿using CrimsonStainedLands.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using static CrimsonStainedLands.Game;

namespace CrimsonStainedLands
{
    public static class TelnetProtocol
    {
        public enum Options : byte
        {
            MudServerStatusProtocolVariable = 1,
            MudServerStatusProtocolValue = 2,
            TelnetType = 24,
            MudServerStatusProtocol = 70,
            SubNegotiationEnd = 240,
            GoAhead = 249,
            SubNegotiation = 250,
            WILL = 251,
            DO = 253,
            InterpretAsCommand = 255,
        }

        public class Command
        {
            public enum Types
            {
                WillTelnetType,
                ClientSendNegotiateType,
                DoMudServerStatusProtocol
            }
            public Types Type { get; set; }

            public Dictionary<string, string[]> Values {
                get;
                set;
            } = new Dictionary<string, string[]>();
        }

        public static bool StartsWith(this byte[] array, byte[] partial, int startindex = 0)
        {
            if (array == null || partial == null || array.Length - startindex < partial.Length) return false;
            var srcarray = ((ReadOnlySpan<byte>)array).Slice(startindex, partial.Length);
            
            return srcarray.SequenceEqual(partial);
        }

        /// <summary>
        /// Server sends to inform that it can handle the telnet type
        /// </summary>
        /// <returns></returns>
        public static readonly byte[] ServerGetDoTelnetType = new byte[] { (byte)Options.InterpretAsCommand, (byte)Options.DO, (byte)Options.TelnetType };

        public static readonly byte[] ClientGetWillTelnetType = new byte[] { (byte)Options.InterpretAsCommand, (byte)Options.WILL, (byte)Options.TelnetType };
        /// <summary>
        /// Server sends when ready to receive telnet type
        /// </summary>
        /// <returns></returns>
        public static readonly byte[] ServerGetWillTelnetType = new byte[] {   
            (byte) Options.InterpretAsCommand,
            (byte) Options.SubNegotiation,
            (byte) Options.TelnetType,
            1,
            (byte) Options.InterpretAsCommand,
            (byte) Options.SubNegotiationEnd};

        public static readonly byte[] ClientNegotiateTelnetType = new byte[] { 
            (byte)Options.InterpretAsCommand, 
            (byte)Options.SubNegotiation, 
            (byte)Options.TelnetType, 
            (byte) 0 }; 

        public static readonly byte[] ServerGetWillMudServerStatusProtocol = new byte[] { 
            (byte)Options.InterpretAsCommand, 
            (byte)Options.WILL, 
            (byte)Options.MudServerStatusProtocol };

        public static readonly byte[] ClientGetWillMudServerStatusProtocol = new byte[] {
            (byte)Options.InterpretAsCommand,
            (byte)Options.DO,
            (byte)Options.MudServerStatusProtocol };

        public static byte[] ServerGetNegotiateMudServerStatusProtocol(Dictionary<string, string[]> Values)
        {
            var data = new MemoryStream();
            data.WriteByte((byte) Options.InterpretAsCommand);
            data.WriteByte((byte)Options.SubNegotiation);
            data.WriteByte((byte)Options.MudServerStatusProtocol);

            foreach(var variable in Values)
            {
                data.WriteByte((byte)Options.MudServerStatusProtocolVariable);
                var name = Encoding.ASCII.GetBytes(variable.Key);
                data.Write(name, 0, name.Length);

                foreach(var value in variable.Value)
                {
                    data.WriteByte((byte)Options.MudServerStatusProtocolValue);
                    var valuebytes = Encoding.ASCII.GetBytes(value);
                    data.Write(valuebytes, 0, valuebytes.Length);
                }

            }

            data.WriteByte((byte)Options.InterpretAsCommand);
            data.WriteByte((byte)Options.SubNegotiationEnd);
            return data.ToArray();
        }

        public static void ProcessInterpretAsCommand(object sender, byte[] data, int position, out int newposition, out byte[] carryover, EventHandler<Command> callback)
        {
            /// IAC TType Negotiation
            /// https://tintin.mudhalla.net/protocols/mtts/
            /// https://tintin.mudhalla.net/rfc/rfc854/
            /// https://www.rfc-editor.org/rfc/rfc1091
            /// https://tintin.mudhalla.net/info/ansicolor/
            
            if (data.Length > position + 2)
            {
                /// DO TTYPE sent on connection acceptance, Client responds with WILL TTYPE
                if (data.StartsWith(ClientGetWillTelnetType, position))
                {
                    /// READY TO RECEIVE TTYPE
                    callback(sender, new Command () { Type = Command.Types.WillTelnetType });
                    carryover = null;
                    newposition = position + 3;
                    return;
                }
                /// TTYPE Received from Client
                else if (data.StartsWith(ClientNegotiateTelnetType, position))
                {
                    bool hittheend = false;
                    var TerminalTypeResponse = new MemoryStream();
                    for (int responseindex = position + ClientNegotiateTelnetType.Length; responseindex < data.Length; responseindex++)
                    {
                        if (data[responseindex] == (byte)Options.InterpretAsCommand)
                        {
                            hittheend = true;
                            break;
                        }
                        TerminalTypeResponse.WriteByte(data[responseindex]);
                    }
                    if (hittheend)
                    {

                        var ClientString = ASCIIEncoding.ASCII.GetString(TerminalTypeResponse.ToArray());
                        callback(sender, new Command()
                        {
                            Type = Command.Types.ClientSendNegotiateType,
                            Values = new Dictionary<string, string[]>() {
                                {"TelnetType", new string[] {ClientString } }
                            }
                        });
                        
                        newposition = position + (int)TerminalTypeResponse.Length + ClientNegotiateTelnetType.Length;
                        carryover = null;
                        return;
                    }
                    else
                    {
                        newposition = data.Length;
                        carryover = new byte[data.Length - position];
                        data.CopyTo(carryover, 0);
                        return;
                    }
                }
                else if(data.StartsWith(ClientGetWillMudServerStatusProtocol, position))
                {
                    newposition = position + ClientGetWillMudServerStatusProtocol.Length;
                    carryover = null;
                    callback(sender, new Command()
                    {
                        Type = Command.Types.DoMudServerStatusProtocol
                    });
                    return;
                }
            }

            newposition = position + 1;
            carryover = null;
        }
    }
}
