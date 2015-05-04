﻿using System;
using OpenTK.Graphics;
using Ninject;
using Protogame;
using Microsoft.Xna.Framework;
using OpenTK.Platform;
using System.IO;
using System.Linq;

namespace ProtogameEditor
{
    public class IDEEditor
    {
        public void Init()
        {
            Console.WriteLine("This is from the editor assembly");
        }

        public string[] GetHandledFileExtensions()
        {
            return new[]
            {
                ".test"
            };
        }

        private EditorGame _game;

        private IKernel _kernel;

        private bool _isSuspended;

        public IKernel CreateEditorKernel<T>() where T : IEmbedContext
        {
            var kernel = new StandardKernel();
            kernel.Load<Protogame3DIoCModule>();
            kernel.Load<ProtogameAssetIoCModule>();
            kernel.Load<ProtogamePlatformingIoCModule>();
            kernel.Load<ProtogameLevelIoCModule>();
            kernel.Load<ProtogameEventsIoCModule>();
            kernel.Bind<ISkin>().To<BasicSkin>();
            kernel.Bind<IBasicSkin>().To<DefaultBasicSkin>();
            kernel.Bind<IEmbedContext>().To<T>().InSingletonScope();
            AssetManagerClient.AcceptArgumentsAndSetup<GameAssetManagerProvider>(kernel, new string[0]);
            return kernel;
        }

        public void OpenWithContext(IGraphicsContext graphicsContext, IWindowInfo windowInfo)
        {
            this.Resume(graphicsContext, windowInfo);
        }

        public bool Update()
        {
            if (_game != null && !_isSuspended)
            {
                _game.RunOneFrame();

                return _game.IsActive;
            }
            else
            {
                return true;
            }
        }

        public void Resize(int width, int height)
        {
            if (_kernel == null)
            {
                return;
            }

            var context = (EditorEmbedContext)_kernel.Get<IEmbedContext>();
            context.Width = width;
            context.Height = height;
            context.TriggerResize();
        }

        public void Suspend()
        {
            _isSuspended = true;
            //_game.Dispose();
            //_game = null;
            //_kernel = null;
        }

        public void Resume(IGraphicsContext graphicsContext, IWindowInfo windowInfo)
        {
            _isSuspended = false;

            if (_game == null)
            {
                _kernel = CreateEditorKernel<EditorEmbedContext>();

                var context = _kernel.Get<IEmbedContext>();

                ((EditorEmbedContext)context).GraphicsContext = graphicsContext;
                ((EditorEmbedContext)context).WindowInfo = windowInfo;

                _game = new EditorGame(_kernel);
            }
        }

        public void Load(string path)
        {
        }

        public void Save(string path)
        {
            using (var writer = new StreamWriter(path))
            {
                foreach (var entity in _game.GameContext.World.Entities.OfType<ISerializableEntity>())
                {
                    writer.WriteLine(entity.Text);
                }
            }
        }
    }
}

