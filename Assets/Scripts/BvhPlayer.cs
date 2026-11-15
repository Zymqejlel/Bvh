using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class BvhPlayer : MonoBehaviour
{
    [Header("UI绑定")]
    public Button loadBtn;
    public Button playPauseBtn;
    public Slider speedSlider;
    public TMP_Text speedText;

    private BvhData bvhData;
    private bool isPlaying = false;
    private float currentFrame = 0;
    private float playSpeed = 1f;
    private Transform skeletonRoot;

    // 用来运行时保存所有骨骼连线信息
    private List<BoneLine> boneLines = new List<BoneLine>();

    private struct BoneLine
    {
        public Transform parentTrans;
        public Transform childTrans;
        public LineRenderer line;
    }

    void Start()
    {
        loadBtn.onClick.AddListener(LoadBvhFile);
        playPauseBtn.onClick.AddListener(SwitchPlayPause);
        speedSlider.onValueChanged.AddListener(OnSpeedChange);
        speedSlider.minValue = 0;
        speedSlider.maxValue = 3;
        speedSlider.value = 1;
    }

    void LoadBvhFile()
    {
        Debug.Log("✅ Load按钮点击");

        if (skeletonRoot != null)
        {
            Destroy(skeletonRoot.gameObject);
        }

        boneLines.Clear();

        string path = Path.Combine(Application.streamingAssetsPath, "test.bvh");
        Debug.Log("📂 读取路径：" + path);

        if (!File.Exists(path))
        {
            Debug.LogError("❌ 找不到test.bvh!检查StreamingAssets文件夹");
            return;
        }

        BvhParser parser = new BvhParser();
        bvhData = parser.Parse(path);
        Debug.Log("🎉 BVH解析成功!根骨骼:" + bvhData.rootJoint.jointName);

        BuildSkeleton(bvhData.rootJoint, null);

        currentFrame = 0;
        isPlaying = false;

        // 先把第0帧姿态写上去
        ApplyFrame(0);
        RebuildAllLines();
    }

    void BuildSkeleton(BvhJoint joint, Transform parent)
    {
        if (joint.isEndSite) return;

        GameObject boneObj = new GameObject(joint.jointName);
        boneObj.transform.SetParent(parent);
        boneObj.transform.localPosition = joint.offset;
        boneObj.transform.localRotation = Quaternion.identity;
        joint.boneTrans = boneObj.transform;

        if (parent == null)
        {
            skeletonRoot = boneObj.transform;
        }

        foreach (var child in joint.children)
        {
            BuildSkeleton(child, boneObj.transform);

            if (!child.isEndSite)
            {
                GameObject lineObj = new GameObject("Line_" + joint.jointName + "_" + child.jointName);
                lineObj.transform.SetParent(boneObj.transform);

                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.2f;
                line.endWidth = 0.2f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Color.magenta;
                line.endColor = Color.magenta;

                BoneLine boneLine = new BoneLine
                {
                    parentTrans = boneObj.transform,
                    childTrans = child.boneTrans,
                    line = line
                };

                boneLines.Add(boneLine);
            }
        }
    }

    void Update()
    {
        if (!isPlaying || bvhData == null) return;

        Debug.Log("▶ 动画帧更新：" + currentFrame.ToString("F2"));

        currentFrame += Time.deltaTime / bvhData.frameTime * playSpeed;

        if (currentFrame >= bvhData.frameCount)
        {
            currentFrame = 0;
        }

        int frameIndex = Mathf.FloorToInt(currentFrame);
        frameIndex = Mathf.Clamp(frameIndex, 0, bvhData.frameCount - 1);

        ApplyFrame(frameIndex);
        RebuildAllLines();
    }

    void ApplyFrame(int frameIdx)
    {
        if (bvhData == null) return;

        float[] data = bvhData.frameValues[frameIdx];
        int ptr = 0;

        ApplyJointRotation(bvhData.rootJoint, data, ref ptr);
    }

    void ApplyJointRotation(BvhJoint j, float[] data, ref int ptr)
    {
        if (j.isEndSite) return;

        Vector3 rot = Vector3.zero;

        for (int i = 0; i < j.channels.Count; i++)
        {
            if (j.channels[i] == "Xrotation") rot.x = data[ptr];
            if (j.channels[i] == "Yrotation") rot.y = data[ptr];
            if (j.channels[i] == "Zrotation") rot.z = data[ptr];
            ptr++;
        }

        // 关键：强制覆盖本地旋转
        j.boneTrans.localRotation = Quaternion.Euler(rot);

        foreach (var child in j.children)
        {
            ApplyJointRotation(child, data, ref ptr);
        }
    }

    // 每帧重绘所有骨骼线，保证跟随骨骼世界位置
    void RebuildAllLines()
    {
        for (int i = 0; i < boneLines.Count; i++)
        {
            BoneLine bl = boneLines[i];

            if (bl.parentTrans != null && bl.childTrans != null && bl.line != null)
            {
                bl.line.SetPosition(0, bl.parentTrans.position);
                bl.line.SetPosition(1, bl.childTrans.position);
            }
        }
    }

    void SwitchPlayPause()
    {
        if (bvhData == null)
        {
            Debug.LogWarning("⚠️ 还没有加载BVH!无法播放");
            return;
        }

        isPlaying = !isPlaying;
        Debug.Log("🔘 播放状态切换!isPlaying = " + isPlaying);
    }

    void OnSpeedChange(float val)
    {
        playSpeed = val;
        speedText.text = $"Speed: {val:F2}";
    }
}