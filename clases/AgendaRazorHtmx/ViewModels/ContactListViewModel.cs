using AgendaRazorHtmx.Models;

namespace AgendaRazorHtmx.ViewModels;

public record ContactListViewModel(
    List<Contact> Contacts,
    int? SelectedId,
    int? DetailIdToLoad = null,
    bool ClearDetail = false
);
