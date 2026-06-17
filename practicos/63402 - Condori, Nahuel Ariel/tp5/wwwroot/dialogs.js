window.agendaDialogs = {
    showModal(dialog) {
        if (dialog && !dialog.open) {
            dialog.showModal();
        }
    }
};
