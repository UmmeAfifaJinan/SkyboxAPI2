using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;

public class CubemapSkyboxManager : MonoBehaviour
{
    private string apiKey = "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"; // Your OpenAI API key
    // private string basePrompt = "a panoramic sea view with a beach in broad daylight";  // Base prompt for all directions
    private string basePrompt = "an HDRI image of a sea with a beach in broad daylight that could be used as a skybox ";
    private string[] prompts = new string[6];
    private string[] generatedImageUrls = new string[6];

    void Start()
    {
        // Dynamically generate the prompts for each direction
        GenerateDynamicPrompts();

        // Call the method to generate the images
        StartCoroutine(GenerateCubemapSkyboxImages());
    }

    // Method to generate dynamic prompts for each cubemap face
    void GenerateDynamicPrompts()
    {
        string[] directions = { "front view", "back view", "left view", "right view", "top view", "bottom view" };

        for (int i = 0; i < directions.Length; i++)
        {
            prompts[i] = basePrompt + " " + directions[i];
        }
    }

    IEnumerator GenerateCubemapSkyboxImages()
    {
        for (int i = 0; i < prompts.Length; i++)
        {
            string prompt = prompts[i];
            string url = "https://api.openai.com/v1/images/generations";

            UnityWebRequest request = new UnityWebRequest(url, "POST");

            // Create the JSON payload for the API request
            string jsonPayload = "{\"prompt\":\"" + prompt + "\", \"n\":1, \"size\":\"1024x1024\"}";

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Set the headers for the request
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // Send the request and wait for the response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Parse the response and extract the image URL
                string jsonResponse = request.downloadHandler.text;
                JObject json = JObject.Parse(jsonResponse);
                generatedImageUrls[i] = json["data"][0]["url"].ToString();
            }
            else
            {
                Debug.LogError("Failed to generate image: " + request.error);
            }
        }

        // Download and apply the images to the cubemap skybox
        StartCoroutine(DownloadAndApplyCubemapSkybox());
    }

    IEnumerator DownloadAndApplyCubemapSkybox()
    {
        Texture2D[] textures = new Texture2D[6];
        for (int i = 0; i < generatedImageUrls.Length; i++)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(generatedImageUrls[i]);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                textures[i] = ((DownloadHandlerTexture)request.downloadHandler).texture;
            }
            else
            {
                Debug.LogError("Failed to download image: " + request.error);
            }
        }

        // Create the cubemap material and assign the textures
        Material skyboxMaterial = new Material(Shader.Find("Skybox/6 Sided"));
        skyboxMaterial.SetTexture("_FrontTex", textures[0]);
        skyboxMaterial.SetTexture("_BackTex", textures[1]);
        skyboxMaterial.SetTexture("_LeftTex", textures[2]);
        skyboxMaterial.SetTexture("_RightTex", textures[3]);
        skyboxMaterial.SetTexture("_UpTex", textures[4]);
        skyboxMaterial.SetTexture("_DownTex", textures[5]);

        // Apply the material to the scene's skybox
        RenderSettings.skybox = skyboxMaterial;

        Debug.Log("Cubemap skybox applied successfully!");
    }
}
