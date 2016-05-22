using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainDevice : MonoBehaviour {

    public Button btnRecord;
    public Mic microphone;
    public ScrollRect scrollLog;
    public Text txtLog;

	// Use this for initialization
	void Start () {
        if (btnRecord) {
            RecordButton recordButton = btnRecord.GetComponent<RecordButton>();
            recordButton.onButtonDown = microphone.RecordStart;
            recordButton.onButtonUp = microphone.RecordEnd;
        }
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void log(string text) {
        txtLog.text += (text + "\n");
        scrollLog.normalizedPosition = new Vector2(0, 0);
    }
}
