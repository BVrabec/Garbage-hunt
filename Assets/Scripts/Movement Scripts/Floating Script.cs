using UnityEngine;

public class Trash : MonoBehaviour
{
    public float floatSpeed = 0.3f;
    public float swayAmount = 0.3f;
    public float swaySpeed = 1.0f;

    private Vector3 startPos;
    private float randomOffset;

    void Start()
    {
        startPos = transform.position;

        // So every trash moves differently
        randomOffset = Random.Range(0f, 8f);
    }

    void Update()
    {
        // Slow left/right sway
        float sway = Mathf.Sin(Time.time * swaySpeed + randomOffset) * swayAmount;

        // Gentle up/down floating
        float vertical = Mathf.Cos(Time.time * 0.8f + randomOffset) * 0.1f;

        transform.position += new Vector3(sway, vertical, 0) * Time.deltaTime;
    }
}