import { Component, input, output } from '@angular/core';
import { ButtonText, ModalTitle, PictureName } from '../../../app.enum';
import { CommonModalTemplate } from '../../../shared/components/modal/common-modal-template/common-modal-template';

@Component({
  selector: 'app-remove-participant-modal',
  templateUrl: './participant-remove-modal.html',
  styleUrl: './participant-remove-modal.scss',
  imports: [CommonModalTemplate],
})
export class RemoveParticipantModal {
  readonly participantId = input.required<string>();
  readonly onRemove = input.required<() => void>();

  readonly closeModal = output<void>();
  readonly buttonAction = output<void>();

  public readonly title = ModalTitle.ParticipantRemove;
  public readonly cancelButtonText = ButtonText.Cancel;
  public readonly buttonText = ButtonText.Remove;
  public readonly subtitle = '';
  public readonly pictureName = PictureName.Cookie;

  public onCloseModal(): void {
    this.closeModal.emit();
  }

  public onActionButtonClick(): void {
    this.onRemove()();
  }
}
