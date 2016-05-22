using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RecordButton : MonoBehaviour {

    public delegate void Callback ();

    public Callback onButtonDown;
    public Callback onButtonUp;
    
    bool isDown;

    // Use this for initialization
    void Start () {
        //canvas = GetComponentInParent<Canvas>();
        isDown = false;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI () {
        Button button = GetComponent<Button>();
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        Rect buttonRect = new Rect(
            new Vector2(rectTransform.position.x, rectTransform.position.y),
            new Vector2(rectTransform.rect.size.x, rectTransform.rect.size.y));

        if (buttonRect.Contains(Input.mousePosition) && Event.current.type == EventType.MouseDown) {
            if (onButtonDown != null) {
                onButtonDown();
            }
            
            isDown = true;
        } else if (Event.current.type == EventType.MouseUp) {
            if (isDown) {
                if (onButtonUp != null) {
                    onButtonUp();
                }

                isDown = false;
            }
        }
    }
}
