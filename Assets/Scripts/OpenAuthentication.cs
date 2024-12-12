using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Images;
using OpenAI.Models;
using UnityEngine;

public class OpenAuthentication : MonoBehaviour
{
    [SerializeField] private List<Texture2D> _textures = new List<Texture2D>();
    private OpenAIClient _client;
    private IReadOnlyList<ImageResult> _imageTask;
    // Start is called before the first frame update
    void Start()
    {
        OpenAIAuthentication authentication = new OpenAIAuthentication().LoadFromEnvironment();
        Debug.Log(authentication.Info);
        _client = new OpenAIClient(authentication);
        //GenerateImage();
    }

    async void GenerateImage()
    {
        ImageGenerationRequest request =
            new ImageGenerationRequest("feminine futurist astronaut on an abandon city from centuries", Model.DallE_2);
        _imageTask = await _client.ImagesEndPoint.GenerateImageAsync(request);
        foreach (ImageResult result in _imageTask)
        {
            Debug.Log(result.Url);
            _textures.Add(result.Texture);
        }
    }
}
