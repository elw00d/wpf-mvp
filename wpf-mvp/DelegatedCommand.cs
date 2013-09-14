using System;

namespace Wpf.Mvp
{
    /// <summary>
    /// Command delegating its executing to another command.
    /// Can be used when one component want to execute commands of another component.
    /// </summary>
    public class DelegatedCommand : BaseCommand
    {
        private BaseCommand command;
        public BaseCommand Command {
            get {
                return command;
            }
            set {
                if (command != null) {
                    throw new NotSupportedException("Resetting is not supported.");
                }
                command = value;
                this.command.CanExecuteChanged += commandOnCanExecuteChanged;
                this.OnCanExecuteChanged();
            }
        }

        public DelegatedCommand(string name)
            : base(name) {
        }

        public DelegatedCommand(string name, BaseCommand command)
            : base(name) {
            if (null == command) throw new ArgumentNullException("command");
            this.command = command;
            this.command.CanExecuteChanged += commandOnCanExecuteChanged;
        }

        private void commandOnCanExecuteChanged(object sender, EventArgs eventArgs) {
            this.OnCanExecuteChanged();
        }

        protected override void ExecuteInternal(object parameter) {
            command.Execute(parameter);
        }

        protected override bool CanExecuteInternal(object parameter) {
            if (command == null) {
                return false;
            }
            return command.CanExecute(parameter);
        }
    }
}
