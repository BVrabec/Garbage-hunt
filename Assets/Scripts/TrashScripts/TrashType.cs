using UnityEngine;

public enum TrashType { Plastic, Glass, Metal, Organic, Other }

public class TrashTypeScript : MonoBehaviour
{
    public TrashType type = TrashType.Plastic; 
}
