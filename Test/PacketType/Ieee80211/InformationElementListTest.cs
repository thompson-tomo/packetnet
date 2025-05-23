﻿/*
This file is part of PacketDotNet.

This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/
/*
 * Copyright 2012 Alan Rushforth <alan.rushforth@gmail.com>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using PacketDotNet;
using PacketDotNet.Ieee80211;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Test.PacketType.Ieee80211;

    [TestFixture]
    public class InformationElementListTest
    {
        private BeaconFrame LoadBeaconFrameFromFile(string file)
        {
            var dev = new CaptureFileReaderDevice(file);
            dev.Open();
            PacketCapture c;
            dev.GetNextPacket(out c);
            var rawCapture = c.GetPacket();
            dev.Close();

            var p = Packet.ParsePacket(rawCapture.GetLinkLayers(), rawCapture.Data);
            var beaconFrame = (BeaconFrame) p.PayloadPacket;
            return beaconFrame;
        }

        [Test]
        public void Test_Bytes_EmptySection()
        {
            var byteArraySegment = new ByteArraySegment(Array.Empty<byte>());
            var ieList = new InformationElementList(byteArraySegment);

            ClassicAssert.AreEqual(0, ieList.Bytes.Length);
        }

        [Test]
        public void Test_Bytes_MultipleInfoElements()
        {
            var ie1 = new InformationElement(
                                             InformationElement.ElementId.WifiProtectedAccess,
                                             new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 });

            var ie2 = new InformationElement(
                                             InformationElement.ElementId.VendorSpecific,
                                             new byte[] { 0xFF, 0xFE, 0xFD, 0xFC });

            var ieList = new InformationElementList { ie1, ie2 };

            byte[] expectedBytes = { 0xD3, 0x5, 0x1, 0x2, 0x3, 0x4, 0x5, 0xDD, 0x4, 0xFF, 0xFE, 0xFD, 0xFC };

            var b = ieList.Bytes;

            ClassicAssert.AreEqual(0xD3, b[0]);
            ClassicAssert.AreEqual(0x5, b[1]);
            ClassicAssert.AreEqual(0x1, b[2]);
            ClassicAssert.AreEqual(0x2, b[3]);
            ClassicAssert.AreEqual(0x3, b[4]);
            ClassicAssert.AreEqual(0x4, b[5]);
            ClassicAssert.AreEqual(0x5, b[6]);
            ClassicAssert.AreEqual(0xDD, b[7]);
            ClassicAssert.AreEqual(0x4, b[8]);
            ClassicAssert.AreEqual(0xFF, b[9]);
            ClassicAssert.AreEqual(0xFE, b[10]);
            ClassicAssert.AreEqual(0xFD, b[11]);
            ClassicAssert.AreEqual(0xFC, b[12]);

            ClassicAssert.IsTrue(ieList.Bytes.SequenceEqual(expectedBytes));
        }

        [Test]
        public void Test_Constructor_BufferTooShortForCompleteValue()
        {
            //The following buffer contains two information elements both with a length of 5
            //but the buffer is too short to contain the complete value for the second IE
            byte[] ieBytes =
            {
                0x00, 0x05, 0x01, 0x02, 0x03, 0x04, 0x05,
                0x00, 0x05, 0x01, 0x02, 0x03
            };

            var byteArraySegment = new ByteArraySegment(ieBytes);

            var ieList = new InformationElementList(byteArraySegment);
            ClassicAssert.AreEqual(2, ieList.Count);

            ClassicAssert.AreEqual(InformationElement.ElementId.ServiceSetIdentity, ieList[0].Id);
            ClassicAssert.AreEqual(5, ieList[0].ValueLength);
            ClassicAssert.AreEqual(5, ieList[0].Value.Length);
            ClassicAssert.AreEqual(7, ieList[0].ElementLength);

            ClassicAssert.AreEqual(InformationElement.ElementId.ServiceSetIdentity, ieList[1].Id);
            ClassicAssert.AreEqual(3, ieList[1].ValueLength);
            ClassicAssert.AreEqual(3, ieList[1].Value.Length);
            ClassicAssert.AreEqual(5, ieList[1].ElementLength);
            ClassicAssert.AreEqual(12, ieList.Length);
        }

        [Test]
        public void Test_Constructor_BufferTooShortForLengthHeader()
        {
            //This buffer contains only enough for the id field (i.e. not length or value)
            byte[] ieBytes = { 0x00 };
            var byteArraySegment = new ByteArraySegment(ieBytes);

            var ieList = new InformationElementList(byteArraySegment);
            ClassicAssert.AreEqual(0, ieList.Count);
        }

        [Test]
        public void Test_Constructor_BufferTooShortForValue()
        {
            //This buffer contains only enough for the id field (i.e. not length or value)
            byte[] ieBytes = { 0x00, 0x01 };
            var byteArraySegment = new ByteArraySegment(ieBytes);

            var ieList = new InformationElementList(byteArraySegment);
            ClassicAssert.AreEqual(1, ieList.Count);
        }

        [Test]
        public void Test_Constructor_EmptyByteArray()
        {
            var byteArraySegment = new ByteArraySegment(Array.Empty<byte>());
            var ieList = new InformationElementList(byteArraySegment);

            ClassicAssert.AreEqual(0, ieList.Count);
            ClassicAssert.AreEqual(0, ieList.Length);
        }

        [Test]
        public void Test_Constructor_EmptyList()
        {
            var ieList = new InformationElementList();

            ClassicAssert.AreEqual(0, ieList.Count);
            ClassicAssert.AreEqual(0, ieList.Length);
        }

        [Test]
        public void Test_Constructor_MultipleInfoElementByteArray()
        {
            //create a dummy info element section for the test. The example here uses 
            //two arbitrary field types (0xD3 & 0xDD). The important information for the test is the length
            //field which is the second byte on each row (0x5 & 0x4). The actual values are meaningless.
            byte[] ieBytes =
            {
                0xD3, 0x05, 0x01, 0x02, 0x03, 0x04, 0x05,
                0xDD, 0x04, 0xFF, 0xFE, 0xFD, 0xFC
            };

            var byteArraySegment = new ByteArraySegment(ieBytes);

            var ieList = new InformationElementList(byteArraySegment);
            ClassicAssert.AreEqual(2, ieList.Count);
            ClassicAssert.AreEqual(13, ieList.Length);

            ClassicAssert.AreEqual(InformationElement.ElementId.WifiProtectedAccess, ieList[0].Id);
            ClassicAssert.AreEqual(5, ieList[0].ValueLength);
            ClassicAssert.IsTrue(ieList[0].Value.SequenceEqual(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 }));

            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, ieList[1].Id);
            ClassicAssert.AreEqual(4, ieList[1].ValueLength);
            ClassicAssert.IsTrue(ieList[1].Value.SequenceEqual(new byte[] { 0xFF, 0xFE, 0xFD, 0xFC }));
        }

        [Test]
        public void Test_Constuctor_FromBeaconFrame()
        {
            var dev = new CaptureFileReaderDevice(NUnitSetupClass.CaptureDirectory + "80211_beacon_frame.pcap");
            dev.Open();
            PacketCapture c;
            dev.GetNextPacket(out c);
            var rawCapture = c.GetPacket();
            dev.Close();

            var p = Packet.ParsePacket(rawCapture.GetLinkLayers(), rawCapture.Data);
            var beaconFrame = (BeaconFrame) p.PayloadPacket;

            List<InformationElement> infoElements = beaconFrame.InformationElements;
            ClassicAssert.AreEqual(InformationElement.ElementId.ServiceSetIdentity, infoElements[0].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.SupportedRates, infoElements[1].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.DsParameterSet, infoElements[2].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.TrafficIndicationMap, infoElements[3].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.ErpInformation, infoElements[4].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.ErpInformation2, infoElements[5].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.RobustSecurityNetwork, infoElements[6].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.ExtendedSupportedRates, infoElements[7].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.HighThroughputCapabilities, infoElements[8].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.HighThroughputInformation, infoElements[9].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[10].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[11].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[12].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[13].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[14].Id);
        }

        [Test]
        public void Test_FindById_ElementNotPresent()
        {
            var beaconFrame = LoadBeaconFrameFromFile(NUnitSetupClass.CaptureDirectory + "80211_beacon_frame.pcap");

            var infoElements = beaconFrame.InformationElements.FindById(InformationElement.ElementId.ChallengeText);
            ClassicAssert.AreEqual(0, infoElements.Length);
        }

        [Test]
        public void Test_FindById_FindRepeatedElement()
        {
            var beaconFrame = LoadBeaconFrameFromFile(NUnitSetupClass.CaptureDirectory + "80211_beacon_frame.pcap");

            var infoElements = beaconFrame.InformationElements.FindById(InformationElement.ElementId.VendorSpecific);

            ClassicAssert.AreEqual(5, infoElements.Length);

            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[0].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[1].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[2].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[3].Id);
            ClassicAssert.AreEqual(InformationElement.ElementId.VendorSpecific, infoElements[4].Id);
        }

        [Test]
        public void Test_FindFirstById_ElementNotPresent()
        {
            var beaconFrame = LoadBeaconFrameFromFile(NUnitSetupClass.CaptureDirectory + "80211_beacon_frame.pcap");

            var infoElement = beaconFrame.InformationElements.FindFirstById(InformationElement.ElementId.ChallengeText);
            ClassicAssert.IsNull(infoElement);
        }

        [Test]
        public void Test_FindFirstById_FindElement()
        {
            var beaconFrame = LoadBeaconFrameFromFile(NUnitSetupClass.CaptureDirectory + "80211_beacon_frame.pcap");

            var infoElement = beaconFrame.InformationElements.FindFirstById(InformationElement.ElementId.DsParameterSet);
            ClassicAssert.AreEqual(InformationElement.ElementId.DsParameterSet, infoElement.Id);
        }
    }