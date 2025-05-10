using System.Collections.Generic;
using UnityEngine;

namespace VSMC
{

    public class ShapeElementWeights
    {
        public float Weight = 1f;
        public EnumAnimationBlendMode BlendMode = EnumAnimationBlendMode.AddAverage;
        public ShapeElementWeights[] ChildElements;
    }

    public class RunningAnimation
    {
        public AnimationMetaData meta;

        public float CurrentFrame;
        public Animation Animation;
        public bool Active;
        public bool Running;
        public int Iterations;

        public bool ShouldRewind = false;
        public bool ShouldPlayTillEnd = false;

        public float EasingFactor;
        public float BlendedWeight;

        public ShapeElementWeights[] ElementWeights;

        /// <summary>
        /// Between 0 and 1
        /// </summary>
        public float AnimProgress => CurrentFrame / (Animation.QuantityFrames - 1);

        public void LoadWeights(ShapeElement[] rootElements)
        {
            ElementWeights = new ShapeElementWeights[rootElements.Length];
            LoadWeights(rootElements, ElementWeights, meta.ElementWeight, meta.ElementBlendMode);
        }

        private void LoadWeights(ShapeElement[] elements, ShapeElementWeights[] intoList, Dictionary<string, float> elementWeight, Dictionary<string, EnumAnimationBlendMode> elementBlendMode)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                ShapeElement elem = elements[i];
                intoList[i] = new ShapeElementWeights();

                if (elementWeight.TryGetValue(elem.Name, out float w))
                {
                    intoList[i].Weight = w;
                }
                else
                {
                    intoList[i].Weight = meta.Weight;
                }

                if (elementBlendMode.TryGetValue(elem.Name, out EnumAnimationBlendMode blendMode))
                {
                    intoList[i].BlendMode = blendMode;
                }
                else
                {
                    intoList[i].BlendMode = meta.BlendMode;
                }


                if (elem.Children != null)
                {
                    intoList[i].ChildElements = new ShapeElementWeights[elem.Children.Length];
                    LoadWeights(elem.Children, intoList[i].ChildElements, elementWeight, elementBlendMode);
                }
            }
        }

        internal void CalcBlendedWeight(float weightSum, EnumAnimationBlendMode blendMode)
        {
            if (weightSum == 0)
            {
                BlendedWeight = EasingFactor;
                return;
            }

            BlendedWeight = Mathf.Clamp(blendMode == EnumAnimationBlendMode.Add ? EasingFactor : EasingFactor / Mathf.Max(meta.WeightCapFactor, weightSum), 0, 1);
        }

        public void Progress(float dt, float walkspeed = 1)
        {
            dt *= meta.GetCurrentAnimationSpeed(walkspeed);

            if ((Active && (Iterations == 0 || Animation.OnAnimationEnd != EnumEntityAnimationEndHandling.EaseOut)) || (Iterations == 0 && Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd))
            {
                EasingFactor = Mathf.Min(1f, EasingFactor + (1f - EasingFactor) * Mathf.Abs(dt) * meta.EaseInSpeed);
            }
            else
            {
                EasingFactor = Mathf.Max(0, EasingFactor - (EasingFactor - 0) * Mathf.Abs(dt) * meta.EaseOutSpeed);
            }

            float newFrame = CurrentFrame + 30 * (ShouldRewind ? -dt : dt) * (Animation.EaseAnimationSpeed ? EasingFactor : 1);

            if (!Active && Animation.OnActivityStopped == EnumEntityActivityStoppedHandling.PlayTillEnd && (Iterations >= 1 || newFrame >= Animation.QuantityFrames - 1))
            {
                EasingFactor = 0;
                CurrentFrame = Animation.QuantityFrames - 1;
                Stop();
                return;
            }

            if ((Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Hold || Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut) && newFrame >= Animation.QuantityFrames - 1 && dt >= 0)
            {
                Iterations = 1;
                CurrentFrame = Animation.QuantityFrames - 1;
                return;
            }

            if ((Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.EaseOut) && newFrame < 0 && dt < 0)
            {
                Iterations = 1;
                CurrentFrame = Animation.QuantityFrames - 1;
                return;
            }


            if (dt >= 0 && newFrame <= 0)
            {
                Iterations--;
                CurrentFrame = 0;
                return;
            }

            CurrentFrame = newFrame;

            if (dt >= 0 && CurrentFrame >= Animation.QuantityFrames) // here and in the modulo used to be a -1 but that skips the last frame (tyron 10dec2020)
            {
                Iterations++;
                CurrentFrame = GameMath.Mod(newFrame, Animation.QuantityFrames);
            }
            if (dt < 0 && CurrentFrame < 0)
            {
                Iterations++;
                CurrentFrame = GameMath.Mod(newFrame, Animation.QuantityFrames);
            }


            if (Animation.OnAnimationEnd == EnumEntityAnimationEndHandling.Stop && Iterations > 0)
            {
                CurrentFrame = Animation.QuantityFrames - 1;
            }
        }

        public void Stop()
        {
            Active = false;
            Running = false;
            CurrentFrame = 0;
            Iterations = 0;
        }

        public void EaseOut(float dt)
        {
            EasingFactor = Mathf.Max(0, EasingFactor - (EasingFactor - 0) * dt * meta.EaseOutSpeed);
        }


    }
}
