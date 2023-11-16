using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace Project.Scripts.Managers
{
    public class AnalyticsManager : MonoBehaviour
    {
        private async void Awake()
        {
            var options = new InitializationOptions().SetEnvironmentName("test");
            try
            {
                await UnityServices.InitializeAsync(options);
            
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            DontDestroyOnLoad(gameObject);
        }
    }
}