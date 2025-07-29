using System.Collections;

using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Reflection;

namespace sapra.InfiniteLands.Tests
{
    public class FloatingOriginTest
    {    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        private static Vector3[] _positions = {
            new Vector3(1010.123f,0,0), 
            new Vector3(0,-float.MaxValue,3),
            new Vector3(-float.MinValue,float.MaxValue,443),
        };
        private static Vector3[] _rotations = {
            new Vector3(0,0,0),
            new Vector3(0,0,45),
            new Vector3(79,0,-45),
            new Vector3(float.MaxValue,float.MinValue, 12.12f),
        };

        private static float[] _distances = {
            10,1000,float.MaxValue,float.MinValue, float.NaN
        };

        private GameObject gameObject;
        private GameObject point;

        private FloatingOrigin floatingOrigin;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject();
            floatingOrigin = gameObject.AddComponent<FloatingOrigin>();

            point = new GameObject();
        }
        
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(point);
        }

        [Test]
        public void Initializes_AtZeroPosition()
        {
            Assert.AreEqual(Vector3.zero, floatingOrigin.transform.position);
        }


        [Test]
        public void MovesOrigin_WhenBeyondMaxDistance()
        {
            GameObject referenceObject = new GameObject();
            floatingOrigin.SetOriginReference(referenceObject.transform);
            floatingOrigin.SetMaxDistance(10);
            
            bool eventCalled = false;
            floatingOrigin.OnOriginMove += (newOrigin, oldOrigin) => { eventCalled = true; };

            // Simulate moving the reference beyond the max distance
            referenceObject.transform.position = new Vector3(15, 0, 0);

            MethodInfo method = floatingOrigin.GetType().GetMethod("LateUpdate",  BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(floatingOrigin, new object[] {});

            Assert.IsTrue(eventCalled);
        }

        [Test]
        public void GetsCurrentOrigin_Correctly()
        {
            Vector3Double expectedOrigin = new Vector3Double(1000, 1000, 1000);
            point.transform.position = expectedOrigin;
            Assert.AreEqual(new Vector3Double(0,0,0), floatingOrigin.GetCurrentOrigin());

            floatingOrigin.SetOriginReference(point.transform);
            floatingOrigin.SetMaxDistance(1000);

            MethodInfo method = floatingOrigin.GetType().GetMethod("LateUpdate",  BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(floatingOrigin, new object[] {});

            Assert.AreEqual(expectedOrigin, floatingOrigin.GetCurrentOrigin());
        }

        [Test]
        public void SetsMaxDistance_Correctly()
        {
            floatingOrigin.SetMaxDistance(500);
            Assert.AreEqual(500, floatingOrigin.SetMaxDistance(500));
        }

        [Test]
        public void Invokes_OnOriginMove_WithCorrectValues()
        {
            GameObject referenceObject = new GameObject();
            floatingOrigin.SetOriginReference(referenceObject.transform);
            floatingOrigin.SetMaxDistance(10);

            Vector3Double oldOrigin = new Vector3Double(0, 0, 0);
            Vector3Double newOrigin = new Vector3Double(15, 0, 0);

            Vector3Double capturedOldOrigin = default;
            Vector3Double capturedNewOrigin = default;

            floatingOrigin.OnOriginMove += (newO, oldO) =>
            {
                capturedNewOrigin = newO;
                capturedOldOrigin = oldO;
            };

            // Move reference beyond max distance
            referenceObject.transform.position = new Vector3(15, 0, 0);
            
            MethodInfo method = floatingOrigin.GetType().GetMethod("LateUpdate",  BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(floatingOrigin, new object[] { });

            Assert.AreEqual(oldOrigin, capturedOldOrigin);
            Assert.AreEqual(newOrigin, capturedNewOrigin);
        }
        [Test]
        public void IMoveOrigin([ValueSource(nameof(_positions))]Vector3 targetPosition, [ValueSource(nameof(_rotations))]Vector3 originalEulers, [ValueSource(nameof(_distances))] float distances)
        {
            //Initialize the scene
            floatingOrigin.transform.rotation = Quaternion.Euler(originalEulers);

            floatingOrigin.SetOriginReference(point.transform);
            floatingOrigin.SetMaxDistance(distances);

            //Move the player
            point.transform.position = targetPosition;
            floatingOrigin.OnOriginMove += (newO, oldO) =>{
                point.transform.position += (oldO-newO);
            };

            MethodInfo method = floatingOrigin.GetType().GetMethod("LateUpdate",  BindingFlags.NonPublic | BindingFlags.Instance);            
            method.Invoke(floatingOrigin, new object[] { });

            float distance = Vector3.Distance(targetPosition, Vector3.zero);
            Vector3 originPosition = distance > distances ? targetPosition : Vector3.zero;

            Vector3Double og = floatingOrigin.GetCurrentOrigin();
            Assert.AreEqual(originPosition, new Vector3((float)og.x, (float)og.y, (float)og.z), "Origin");
            
            Vector3 pointPosition = distance > distances ? targetPosition-originPosition:targetPosition;
            Assert.AreEqual(pointPosition, point.transform.position, "Point Position");
        }

    }
}