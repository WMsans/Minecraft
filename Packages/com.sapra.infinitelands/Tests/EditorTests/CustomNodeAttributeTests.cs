using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.Networking;
using System;

namespace sapra.InfiniteLands.Tests
{
    public class CustomNodeAttributeTests
    {
        [UnityTest]
        public IEnumerator TestCustomNodeDocsURLs()
        {
            // Get all assemblies in the project
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Find all classes with CustomNodeAttribute
            var typesWithAttribute = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && 
                              type.GetCustomAttributes(typeof(CustomNodeAttribute), false).Length > 0);

            bool allValid = true;
            foreach (var type in typesWithAttribute)
            {
                var attribute = type.GetCustomAttribute<CustomNodeAttribute>();
                
                // Check if docs is empty
                if (string.IsNullOrEmpty(attribute.docs))
                {
                    Debug.Log($"Class {type.Name} has empty docs URL");
                    allValid = false;
                    continue;
                }

                // Basic URL format check
                if (!attribute.docs.StartsWith("http://") && !attribute.docs.StartsWith("https://"))
                {
                    Debug.Log($"Class {type.Name} has invalid docs URL format: {attribute.docs}");
                    allValid = false;
                    continue;
                }

                // Check URL with UnityWebRequest
                UnityWebRequest request = UnityWebRequest.Get(attribute.docs);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    if (request.responseCode == 404)
                    {
                        Debug.Log($"Class {type.Name} docs URL returned 404: {attribute.docs}");
                    }
                    else
                    {
                        Debug.Log($"Class {type.Name} docs URL failed with {request.responseCode}: {attribute.docs}");
                    }
                    allValid = false;
                }
                else
                {
                    //Debug.Log($"Valid docs URL for {type.Name}: {attribute.docs}");
                }
            }

            Assert.IsTrue(allValid, "Some CustomNodeAttribute docs URLs were invalid or returned 404");
        }
    }
}