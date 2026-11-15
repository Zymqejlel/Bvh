using System;
using UnityEngine;

public class BvhParser
{
    private string[] tokens;
    private int tokenIndex;

    public BvhData Parse(string filePath)
    {
        string text = System.IO.File.ReadAllText(filePath);
        tokens = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        tokenIndex = 0;
        BvhData data = new BvhData();

        ReadToken("HIERARCHY");
        data.rootJoint = ReadJoint();

        ReadToken("MOTION");
        ReadToken("Frames:");
        data.frameCount = int.Parse(ReadToken());
        ReadToken("Frame");
        ReadToken("Time:");
        data.frameTime = float.Parse(ReadToken());

        for (int f = 0; f < data.frameCount; f++)
        {
            float[] frameData = new float[GetTotalChannelCount(data.rootJoint)];
            for (int i = 0; i < frameData.Length; i++)
            {
                frameData[i] = float.Parse(ReadToken());
            }
            data.frameValues.Add(frameData);
        }
        return data;
    }

    private BvhJoint ReadJoint()
    {
        BvhJoint joint = new BvhJoint();
        string type = ReadToken();
        joint.jointName = ReadToken();
        joint.isEndSite = (type == "End");
        ReadToken("{");
        while (PeekToken() != "}")
        {
            if (PeekToken() == "OFFSET")
            {
                ReadToken("OFFSET");
                joint.offset = new Vector3(float.Parse(ReadToken()), float.Parse(ReadToken()), float.Parse(ReadToken()));
            }
            else if (PeekToken() == "CHANNELS")
            {
                ReadToken("CHANNELS");
                int channelNum = int.Parse(ReadToken());
                for (int i = 0; i < channelNum; i++)
                {
                    joint.channels.Add(ReadToken());
                }
            }
            else if (PeekToken() == "JOINT" || PeekToken() == "End")
            {
                joint.children.Add(ReadJoint());
            }
            else
            {
                tokenIndex++;
            }
        }
        ReadToken("}");
        return joint;
    }

    private int GetTotalChannelCount(BvhJoint j)
    {
        int cnt = j.channels.Count;
        foreach (var child in j.children) cnt += GetTotalChannelCount(child);
        return cnt;
    }

    private string ReadToken() => tokens[tokenIndex++];
    private void ReadToken(string s)
    {
        string t = ReadToken();
        if (t != s) Debug.LogError("解析出错，预期：" + s + "，实际读到：" + t);
    }
    private string PeekToken() => tokens[tokenIndex];
}