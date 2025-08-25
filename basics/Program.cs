﻿using Microsoft.VisualBasic;
using Microsoft.Windows.Devices.Midi2;
//using Microsoft.Windows.Devices.Midi2.Initialization;
using Microsoft.Windows.Devices.Midi2.Diagnostics;
using Microsoft.Windows.Devices.Midi2.Messages;
using SoundPlayer.Services;
using System;
using System.Net;
using Windows.Devices.Enumeration;

// for comments on what each step does, please see the C++/WinRT sample
// the code is almost identical

Console.WriteLine("Checking for Windows MIDI Services");

var player = new MidiPlayer();
bool onFirst = true;

// the initializer implements the Dispose pattern, so will shut down WinRT type redirection when disposed
using (var initializer = Microsoft.Windows.Devices.Midi2.Initialization.MidiDesktopAppSdkInitializer.Create())
{
    // initialize SDK runtime
    if (!initializer.InitializeSdkRuntime())
    {
        Console.WriteLine("Failed to initialize SDK Runtime");
        return 1;
    }

    // start the service
    if (!initializer.EnsureServiceAvailable())
    {
        Console.WriteLine("Failed to get service running");

        return 1;
    }

    Console.WriteLine("Creating session");

    // session implements IDisposable
    using (MidiSession session = MidiSession.Create("API Sample Session"))
    {
        var endpointAId = MidiDiagnostics.DiagnosticsLoopbackAEndpointDeviceId;
        var endpointBId = "\\\\?\\swd#midisrv#midiu_ksa_13163318260182139568#{e7cce071-3c03-423f-88d3-f1045d02552b}";// MidiDiagnostics.DiagnosticsLoopbackBEndpointDeviceId;

        Console.WriteLine("Connecting to Sender UMP Endpoint: " + endpointAId);
        Console.WriteLine("Connecting to Receiver UMP Endpoint: " + endpointBId);


        var sendEndpoint = session.CreateEndpointConnection(endpointAId);
        var receiveEndpoint = session.CreateEndpointConnection(endpointBId);

        

    // c# allows local functions. This is nicer than anonymous because we can unregister it by name
    void MessageReceivedHandler(IMidiMessageReceivedEventSource sender, MidiMessageReceivedEventArgs args)
        {
            var ump = args.GetMessagePacket();

            //Console.WriteLine();
            //Console.WriteLine("Received UMP");
            //Console.WriteLine("- Current Timestamp: " + MidiClock.Now);
            //Console.WriteLine("- UMP Timestamp:     " + ump.Timestamp);
            //Console.WriteLine("- UMP Msg Type:      " + ump.MessageType);
            //Console.WriteLine("- UMP Packet Type:   " + ump.PacketType);
            //Console.WriteLine("- Message:           " + MidiMessageHelper.GetMessageDisplayNameFromFirstWord(args.PeekFirstWord()));

            if (ump is MidiMessage32)
            {
                var ump32 = ump as MidiMessage32;

                if (ump32 != null) {
                    //Console.WriteLine("- Word 0:            0x{0:X}", ump32.Word0);
                    uint value = ump32.Word0;

                    // Extract individual bytes
                    byte byte0 = (byte)(value & 0xFF);         // Least significant byte (LSB)
                    byte byte1 = (byte)((value >> 8) & 0xFF);  // Second byte
                    byte byte2 = (byte)((value >> 16) & 0xFF); // Third byte
                    byte byte3 = (byte)((value >> 24) & 0xFF); // Most significant byte (MSB)

                    Console.WriteLine("- Note  :" + byte1);

                    if (onFirst)
                    {
                        onFirst = false;
                        player.PlayNote((int)byte1, 80);
                    }
                    else
                    {
                        onFirst = true;
                    }
                }
            }
        };

        player.PlayNote((int)0, 50);
        receiveEndpoint.MessageReceived += MessageReceivedHandler;

        Console.WriteLine("Opening endpoint connection");

        // once you have wired up all your event handlers, added any filters/listeners, etc.
        // You can open the connection. Doing this will query the cache for the in-protocol 
        // endpoint information and function blocks. If not there, it will send out the requests
        // which will come back asynchronously with responses.
        if (!receiveEndpoint.Open())
        {
            Console.WriteLine("Could not open receive endpoint");
            return 1;
        }

        if (!sendEndpoint.Open())
        {
            Console.WriteLine("Could not open send endpoint");
            return 1;
        }

        Console.WriteLine("Creating MIDI 1.0 Channel Voice 32-bit UMP...");

        var ump32 = MidiMessageBuilder.BuildMidi1ChannelVoiceMessage(
            MidiClock.Now, // use current timestamp
            new MidiGroup(5),      // group 5
            Midi1ChannelVoiceMessageStatus.NoteOn,  // 9
            new MidiChannel(3),      // channel 3
            120,    // note 120 - hex 0x78
            100);   // velocity 100 hex 0x64

        sendEndpoint.SendSingleMessagePacket((IMidiUniversalPacket)ump32);  // could also use the SendWords methods, etc.

        Console.WriteLine(" ** Wait for the message to arrive, and then press enter to cleanup. ** ");
        Console.ReadLine();

        // you should unregister the event handler as well
        receiveEndpoint.MessageReceived -= MessageReceivedHandler;

        // not strictly necessary if the session is going out of scope or is in a using block
        session.DisconnectEndpointConnection(sendEndpoint.ConnectionId);
        session.DisconnectEndpointConnection(receiveEndpoint.ConnectionId);
    }

    return 0;
}
