() => {
    $.ajax({
        url: Strings.URL_CLASS + "joinClass/" + classe.id, // api/Class/joinClass
        contentType: "application/json",
        method: "POST",
        success: (classe: Types.IClass) => {
            updClassDispatch({
                type: Strings.UPDATE_CLASS_PARAMS,
                classAction: Strings.EDIT_CLASS,
                classId: classe.id,
                class: classe
            })
        },
        error: (xhr, status, error) => {
            console.log(xhr.responseText, status, error)
        }
    })
}