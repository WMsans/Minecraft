using UnityEngine;

namespace sapra.InfiniteLands{
    [RequireComponent(typeof(LineRenderer))]
    [ExecuteAlways]
    public class MarkMe : MonoBehaviour
    {
        private LineRenderer lineRenderer;
        public float maxHeight = 1000f;       
        public float maxDistance = 1000f;    
        public float minDistance = 800;   
        private Transform player; 
        public Color lineColor = Color.white;
        public float lineWidth = 25;

        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
            
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            player = Camera.main.transform;
            UpdateLinePosition();
        }

        void Update()
        {
            UpdateLinePosition();
            UpdateLineTransparency();
        }

        void UpdateLinePosition()
        {
            Vector3 startPos = new Vector3(transform.position.x, transform.position.y + 200, transform.position.z);
            Vector3 endPos = new Vector3(startPos.x, startPos.y + maxHeight, startPos.z);
            
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }

        void UpdateLineTransparency()
        {
            if (player == null) return;

            // Calculate distance between player and object
            float distance = Vector3.Distance(player.position, transform.position);
            
            // Calculate alpha based on distance (0 = transparent, 1 = opaque)
            float alpha = Mathf.InverseLerp(minDistance, maxDistance, distance);
            alpha = Mathf.Clamp01(alpha);
            
            // Apply color with calculated alpha
            Color newColor = new Color(lineColor.r, lineColor.g, lineColor.b, alpha);
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }
}