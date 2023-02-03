using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadProjectorViewer.ViewModel.Scenes
{
    public partial class SceneModel
    {
        public bool ClearBeforAdd
        {
            get => this.ProjectScene.ClearBeforAdd;
            set
            {
                this.ProjectScene.ClearBeforAdd = value;
                OnPropertyChanged("ClearBeforAdd");
            }
        }

        /// Tranform
        public double DefaultScaleX
        {
            get => this.ProjectScene.DefaultScaleX;
            set
            {
                this.ProjectScene.DefaultScaleX = value;
                OnPropertyChanged("DefaultScaleX");
            }
        }
        public double DefaultScaleY
        {
            get => this.ProjectScene.DefaultScaleY;
            set
            {
                this.ProjectScene.DefaultScaleY = value;
                OnPropertyChanged("DefaultScaleY");
            }
        }
        public double DefaultAngle
        {
            get => this.ProjectScene.DefaultAngle;
            set
            {
                this.ProjectScene.DefaultAngle = value;
                OnPropertyChanged("DefaultAngle");
            }
        }

        ///Bool
        ///
        public bool DefaultMirror
        {
            get => this.ProjectScene.DefaultMirror;
            set
            {
                this.ProjectScene.DefaultMirror = value;
                OnPropertyChanged("DefaultMirror");
            }
        }
        public bool StepByStep
        {
            get => this.ProjectScene.StepByStep;
            set
            {
                this.ProjectScene.StepByStep = value;
                OnPropertyChanged("StepByStep");
            }
        }
    }
}
