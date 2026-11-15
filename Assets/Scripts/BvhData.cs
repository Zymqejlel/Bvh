using System.Collections.Generic;
using UnityEngine;

public class BvhJoint
{
    public string jointName;
    public Vector3 offset;
    public List<string> channels = new List<string>();
    public List<BvhJoint> children = new List<BvhJoint>();
    public Transform boneTrans;
    public bool isEndSite;  // 新增：标记是不是End末端点，末端不生成骨骼物体
}

public class BvhData
{
    public BvhJoint rootJoint;
    public int frameCount;
    public float frameTime;
    public List<float[]> frameValues = new List<float[]>();
}