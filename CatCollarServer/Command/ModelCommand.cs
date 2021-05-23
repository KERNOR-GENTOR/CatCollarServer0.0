﻿using CatCollarServer.Audio;
using CatCollarServer.AudioModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.Command
{
    public static class ModelCommand
    {
        public static void WriteList(ref Context context)
        {
            Dictionary<uint, Model> models;
            if (context.Storage.Models == null)
            {
                models = new Dictionary<uint, Model>();
            }
            else
            {
                models = new Dictionary<uint, Model>(context.Storage.Models);
            }
            if (models.Count > 0)
            {
                Console.WriteLine("Available models are:");
                foreach(var model in models)
                {
                    Console.WriteLine(" - \"{0}\" ({1})", model.Value.Text, model.Value.Samples.Count);
                }
            }
            else
            {
                Console.WriteLine("There are no any models in the storage");
            }
        }

        public static void Add(ref Context context, in string modelName)
        {
            // Check the model's name
            if (string.IsNullOrEmpty(modelName))
            {
                Console.WriteLine("Model name is not specified");
                return;
            }

            Console.WriteLine("Adding the new sample");

            // Get a word to recognize
            Word word = GetWord(ref context);
            if (word == null)
            {
                return;
            }

            // Inin storage
            if (context.Storage.Init())
            {
                return;
            }

            // Find the model
            Model model = null;
            Dictionary<uint, Model> models = new Dictionary<uint, Model>(context.Storage.Models);
            foreach (var m in models)
            {
                if(modelName == m.Value.Text)
                {
                    model = m.Value;
                    break;
                }
            }

            // Create the model if it does not exist
            if (model == null)
            {
                model = new Model(modelName);
                context.Storage.AddModel(model);
            }

            // Add the sample to the model
            context.Storage.AddSample(model.Id, word);
            context.Storage.Persist();

            Console.WriteLine("The new sample has been successfully added!");
        }
        
        public static string Recognize(ref Context context, in string modelNameChar)
        {
            // Check the storage
            if(!context.Storage.Init())
            {
                return null;
            }
            if (context.Storage.Models.Count == 0)
            {
                Console.WriteLine("Models storage is empty! Add some model before starting recognition.");
                return null;
            }

            Console.WriteLine("Word recognition");

            // Get a word to recognize
            Word word = GetWord(ref context);

            //Get available models
            List<Model> modelsFiltered = new List<Model>();
            Dictionary<uint, Model> models = context.Storage.Models;

            List<string> modelNames = new List<string>();
            if(modelNameChar != null)
            {
                modelNames = modelNameChar.Split(',').ToList();
            }

            foreach(var m in models)
            {
                string modelName = m.Value.Text;
                if(modelNames.Count == 0 || modelNames.FirstOrDefault(i => i == modelName) != modelNames.LastOrDefault())
                {
                    modelsFiltered.Add(m.Value);
                }
            }

            // Try to recognize
            Recognizer recognizer = new Recognizer(modelsFiltered);
            Model model = recognizer.Do(word);

            //Get result
            if(model != null)
            {
                return model.Text;
            }

            return "--no result--";
        }

        private static Word GetWord(ref Context context)
        {
            //Check pre-requirements
            if(context.WavData == null)
            {
                Console.WriteLine("Input data is not specified");
                return null;
            }

            Console.WriteLine("Checking input data");

            // Create the Processor
            Processor processor = new Processor(context.WavData);
            processor.Init();
            context.Processor = processor;

            Console.WriteLine("Calculating MFCC for input data");

            // Calc & show mfcc
            Word word = processor.GetAsWholeWord();
            processor.InitMFCC(ref word);

            return word;
        }
    }
}