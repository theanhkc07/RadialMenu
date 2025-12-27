using System.ComponentModel;
using System;
using RadialMenuPlugin.Controls.Buttons.Shaped.Base.Types.Images;

namespace RadialMenuPlugin.Controls.Buttons.Shaped.Base
{
    /// <summary>
    /// 
    /// </summary>
    public class BaseShapedButton : ShapedButton, INotifyPropertyChanged
    {
        #region Protected/Private Properties
        #endregion

        #region Public properties
        #endregion

        #region Public methods
        #endregion

        #region  Protected/Private Methods
        protected override void AnimationNormalHandler()
        {
            try
            {
                Buttons[ShapedButtonImageNames.Default].Alpha = ImageList[ShapedButtonImageNames.Default].Alpha;
                Buttons[ShapedButtonImageNames.Selected].Alpha = 0;
                Buttons[ShapedButtonImageNames.disabled].Alpha = 0;
                Buttons[ShapedButtonImageNames.Hover].Alpha = 0;
            }
            catch (Exception e) { Logger.Error(e); }
        }
        protected override void AnimationHoverHandler()
        {
            try
            {
                Buttons[ShapedButtonImageNames.Default].Alpha = 0;
                Buttons[ShapedButtonImageNames.Selected].Alpha = 0;
                Buttons[ShapedButtonImageNames.disabled].Alpha = 0;
                Buttons[ShapedButtonImageNames.Hover].Alpha = ImageList[ShapedButtonImageNames.Hover].Alpha;
            }
            catch (Exception e) { Logger.Error(e); }
        }
        protected override void AnimationSelectedHandler()
        {
            try
            {
                Buttons[ShapedButtonImageNames.Default].Alpha = 0;
                Buttons[ShapedButtonImageNames.Selected].Alpha = ImageList[ShapedButtonImageNames.Selected].Alpha;
                Buttons[ShapedButtonImageNames.disabled].Alpha = 0;
                Buttons[ShapedButtonImageNames.Hover].Alpha = 0;
            }
            catch (Exception e) { Logger.Error(e); }
        }
        protected override void AnimationDisabledHandler()
        {
            try
            {
                Buttons[ShapedButtonImageNames.Default].Alpha = 0;
                Buttons[ShapedButtonImageNames.Selected].Alpha = 0;
                Buttons[ShapedButtonImageNames.disabled].Alpha = ImageList[ShapedButtonImageNames.disabled].Alpha;
                Buttons[ShapedButtonImageNames.Hover].Alpha = 0;
            }
            catch (Exception e) { Logger.Error(e); }
        }
        #endregion
    }
}
