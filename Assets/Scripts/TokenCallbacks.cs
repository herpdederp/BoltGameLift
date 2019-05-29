using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UdpKit;
using System;

public class TestToken : Bolt.IProtocolToken
{
    public String ArbitraryData;
    public String password;

    public void Read(UdpPacket packet)
    {
        ArbitraryData = packet.ReadString();
        password = packet.ReadString();
    }

    public void Write(UdpPacket packet)
    {
        packet.WriteString(ArbitraryData);
        packet.WriteString(password);
    }
}


[BoltGlobalBehaviour]
public class TokenCallbacks : Bolt.GlobalEventListener
{
    public override void BoltStartBegin()
    {
        BoltNetwork.RegisterTokenClass<TestToken>();
    }
}
