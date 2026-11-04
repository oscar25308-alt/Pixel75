using UnityEngine;

// Recorre los cuadros (frames) de un spritesheet para animar el sprite.
public class SpriteAnimado : MonoBehaviour
{
    public Sprite[] cuadros;     // frames de la animación
    public float fps = 8f;       // cuadros por segundo

    private SpriteRenderer dibujo;
    private float tiempo;

    void Start()
    {
        dibujo = GetComponent<SpriteRenderer>();
        if (cuadros != null && cuadros.Length > 0)
            tiempo = Random.value * cuadros.Length / fps;   // desfase para que no vayan todos iguales
    }

    void Update()
    {
        if (cuadros == null || cuadros.Length == 0) return;
        tiempo += Time.deltaTime;
        int indice = (int)(tiempo * fps) % cuadros.Length;
        dibujo.sprite = cuadros[indice];
    }
}
