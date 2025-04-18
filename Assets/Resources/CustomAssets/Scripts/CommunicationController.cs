using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class CommunicationController : MonoBehaviour
{
    public List<string> motionInfo;
    public List<string> testInfo;

    public float hotVoltage;
    public float coldVoltage;
    private float oldHotVoltage;
    private float oldColdVoltage;

    public SignalSender signalSender;

    string testDataPath = "Resources\\MotionData\\testData.txt";
    string demoDataPath = "Resources\\MotionData\\demoData.txt";
    string regex = "(?<=\"thermalKeypoints\":\\[\"\\[.*?, .*?, .*?, .*?, )(-?\\d*\\.?\\d+)?(?=\\])";
    //string regex = "(?<=\"thermalKeypoints\":\\[\"\\[.*?,.*?,.*?,.*?,)(-?\\d*\\.?\\d+)";
    //string regex = "(?<=\\[.*?, )\\d+(\\.\\d+)?(?=\\])"


    // Start is called before the first frame update
    void Start()
    {
        ReadMotionData();
        signalSender = GetComponent<SignalSender>();

        oldHotVoltage = hotVoltage;
        oldColdVoltage = coldVoltage;

        UpdateVoltage(true);
        UpdateVoltage(false);
        UpdateTestVoltage();
    }

    // Update is called once per frame
    void Update()
    {
        if (hotVoltage != oldHotVoltage)
        {
            oldHotVoltage = hotVoltage;
            //UpdateVoltage(true);
            //UpdateTestVoltage();
        }

        if (coldVoltage != oldColdVoltage)
        {
            oldColdVoltage = coldVoltage;
            //UpdateVoltage(false);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            InitiateVoltageTest();
        }

        if (Input.GetKey(KeyCode.Q) && Input.GetKeyDown(KeyCode.Alpha1))
        {
            SendMotionInfo(0);
        }
        if (Input.GetKey(KeyCode.Q) && Input.GetKeyDown(KeyCode.Alpha2))
        {
            SendMotionInfo(1);
        }
        if (Input.GetKey(KeyCode.Q) && Input.GetKeyDown(KeyCode.Alpha3))
        {
            SendMotionInfo(2);
        }
        if (Input.GetKey(KeyCode.Q) && Input.GetKeyDown(KeyCode.Alpha4))
        {
            SendMotionInfo(3);
        }

    }

    public void ReadMotionData()
    {
        // Check if the file exists
        if (File.Exists(testDataPath))
        {
            // Open the file for reading
            using (StreamReader reader = new StreamReader(testDataPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    motionInfo.Add(line.ToString());
                }
            }
        }
        else
        {
            print("File not found: " + testDataPath);
        }

        if (File.Exists(demoDataPath))
        {
            // Open the file for reading
            using (StreamReader reader = new StreamReader(demoDataPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    testInfo.Add(line.ToString());
                }
            }
        }
        else
        {
            print("File not found: " + demoDataPath);
        }
    }

    public void UpdateVoltage(bool hot)
    {
        if (hot)
        {
            motionInfo[2] = Regex.Replace(motionInfo[2], regex, hotVoltage.ToString());
            motionInfo[3] = Regex.Replace(motionInfo[3], regex, hotVoltage.ToString());
        }
        else
        {
            motionInfo[0] = Regex.Replace(motionInfo[0], regex, coldVoltage.ToString());
            motionInfo[1] = Regex.Replace(motionInfo[1], regex, coldVoltage.ToString());
        }
    }

    public void UpdateTestVoltage()
    {
        testInfo[0] = Regex.Replace(testInfo[0], regex, hotVoltage.ToString());
    }

    public void SendMotionInfo(int line)
    {
        Debug.Log(motionInfo[line]);
        signalSender.SendSignal("{" + String.Format(motionInfo[line][1..(motionInfo[line].Length - 1)], GetVoltageString(line)) + "}");
    }

    public void InitiateVoltageTest()
    {
        signalSender.SendSignal("{" + String.Format(testInfo[0][1..(testInfo[0].Length - 1)], GetVoltageString(2)) + "}");
    }

    public string GetVoltageString(int line)
    {
        if (line == 0 || line == 1)
        {
            return coldVoltage.ToString();
        }
        else if (line == 2 || line == 3)
        {
            return hotVoltage.ToString();
        }
        return 0.ToString();
    }

    public IEnumerator LimitVoltage()
    {
        Debug.Log("Lowering voltage");
        //hotVoltage -= .3f;
        //coldVoltage -= .3f;
        yield return new WaitForSeconds(8f);
        Debug.Log("Raising voltage");
        //hotVoltage += .3f;
        //coldVoltage += .3f;
    }
}
