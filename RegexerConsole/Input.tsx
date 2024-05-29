                    $.ajax({
                        url: Strings.URL_CLASS + "ratings/" + classe.id,
                        contentType: "application/json",
                        data: { count: Integers.OPTS_DOM_LIMIT, ref1Id, ref2Id, ref3Id, ref4Id, ref5Id } as Types.IRatsLoadParams,
                        method: "GET",
                        success: (result: Types.IInfScrollResult<Types.IOptRow>) => {
                            resolve(result)
                        },
                        error: (xhr, status, error) => {
                            console.log(xhr.responseText, status, error)
                        }
                    })