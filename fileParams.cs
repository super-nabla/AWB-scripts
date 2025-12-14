public string ProcessArticle(string ArticleText, string ArticleTitle, int wikiNamespace, out string Summary, out bool Skip)
    {
      return processFileParams(ArticleText, ArticleTitle, wikiNamespace, out Summary, out Skip);
    }

public string processFileParams(string ArticleText, string ArticleTitle, int wikiNamespace, out string Summary, out bool Skip)
    {
        Skip = false;
        
        // Mappa delle sostituzioni dei parametri inglesi in italiano
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"border", "bordo"}, {"bottom", "sotto"}, {"center", "centro"}, {"centre", "centro"},
            {"frameless", "senza cornice"}, {"frame", "riquadrato"}, {"framed", "riquadrato"}, 
            {"enframed", "riquadrato"}, {"originale", "riquadrato"}, {"incorniciato", "riquadrato"}, 
            {"left", "sinistra"}, {"middle", "metà"}, {"miniatura", "min"}, {"none", "nessuno"}, 
            {"right", "destra"}, {"sub", "pedice"}, {"text-bottom", "testo-sotto"}, 
            {"text-top", "testo-sopra"}, {"thumb", "min"}, {"thumbnail", "min"}, 
            {"top", "sopra"}, {"upright", "verticale"},
        };
        
        // Espressione regolare per trovare tutti gli usi dei file [[File:...]] o [[Image:...]]
        string pattern = @"\[\[\s*(File|Immagine|Image)\s*:[^\]]+?\]\]";
        
        int nSubstitutions = 0;
        // Variabile per tracciare se è avvenuta almeno una rimozione di 'px'
        bool pxRemoved = false; 
        
        ArticleText = Regex.Replace(ArticleText, pattern, m => {
            string fileMatch = m.Value;
            
            // Estrae il contenuto senza le [[ e ]] esterne
            string innerContent = fileMatch.Substring(2, fileMatch.Length - 4);
            
            // Separa i token in base al divisore '|'
            var tokens = innerContent.Split('|').ToList();
            
            if (tokens.Count < 2)
            {
                return fileMatch;
            }
            
            // 2. Standardizzazione della parte iniziale (es. "Image:" -> "Immagine:")
            string filePrefix = tokens[0].Trim();
            if (Regex.IsMatch(filePrefix, @"^(Image|image):", RegexOptions.IgnoreCase))
            {
                tokens[0] = Regex.Replace(filePrefix, @"^(Image|image):", "Immagine:", RegexOptions.IgnoreCase);
                nSubstitutions++;
            }
            
            // Lista separata per i parametri di dimensione in 'px'
            string pxParameter = null;
            
            // 3. Analisi e Localizzazione dei Parametri
            var parameters = tokens.Skip(1)
                                   .Select(token => token.Trim())
                                   .ToList();
            var replacedParameters = new List<string>();
            foreach (var param in parameters)
            {
                // Controllo se il parametro è una dimensione assoluta in 'px'
                if (Regex.IsMatch(param, @"^\d+x?\d*px$", RegexOptions.IgnoreCase))
                {
                    pxParameter = param; // Salva il parametro px
                    continue; // NON aggiungerlo ancora a replacedParameters
                }
                if (replacements.ContainsKey(param))
                {
                    nSubstitutions++;
                    replacedParameters.Add(replacements[param]);
                }
                else if (Regex.IsMatch(param, @"^upright=\d*(\.\d+)?$", RegexOptions.IgnoreCase))
                {
                    // Gestione di 'upright=X'
                    nSubstitutions++;
                    replacedParameters.Add(Regex.Replace(param, "upright", "verticale", RegexOptions.IgnoreCase));
                }
                else
                {
                    replacedParameters.Add(param);
                }
            }
            // 4. Post-pulizia e standardizzazione aggiuntiva
            
            // **NUOVA LOGICA PER LA RIMUOVERE IL PX**
            // Se è presente il parametro 'min', ignora la dimensione in px.
            if (pxParameter != null)
            {
                if (!replacedParameters.Contains("min"))
                {
                    // Se NON c'è 'min', mantieni il parametro px
                    replacedParameters.Insert(0, pxParameter); 
                }
                else
                {
                    // Se c'è 'min', rimuovi il px.
                    // nSubstitutions viene incrementato qui per contare la rimozione
                    nSubstitutions++;
                    pxRemoved = true; // Imposta il flag di rimozione avvenuta
                }
            }
            
            // Se c'è 'min', rimuovi 'destra' (Logica originale)
            if (replacedParameters.Contains("min"))
            {
                int indexDestra = replacedParameters.IndexOf("destra");
                if (indexDestra > -1)
                {
                    replacedParameters.RemoveAt(indexDestra);
                }
            }
            
            // Standardizzazione di 'verticale' (Logica originale)
            replacedParameters = replacedParameters.Select(token => {
                if (token == "verticale=.7" || token == "verticale=0.7")
                {
                    return "verticale";
                }
                if (token == "verticale=1" || token == "verticale=1.0")
                {
                    return "";
                }
                return token;
            }).Where(p => !string.IsNullOrEmpty(p))
              .ToList();
              
            // Ricostruisce la stringa del file
            string newInnerContent = tokens[0] + (replacedParameters.Count > 0 ? "|" : "") + string.Join("|", replacedParameters);
            return "[[" + newInnerContent + "]]";
        }, RegexOptions.Singleline);
        
        // Salta l'articolo se non è stata fatta alcuna modifica
        if (nSubstitutions == 0)
        {
            Skip = true;
            // Summary è irrilevante se si salta, ma va comunque inizializzato
            Summary = ""; 
        } else {
            // Aggiorna il riepilogo con il numero effettivo di modifiche
            Summary = string.Format("Localizza e uniforma {0} parametri dei file.", nSubstitutions);
            
            // Aggiunge la frase sul dimensionamento assoluto SE la rimozione del 'px' è avvenuta almeno una volta
            if (pxRemoved)
            {
                Summary += " " + "Il [[WP:RIS|dimensionamento assoluto]] delle immagini è deprecato.";
            }
        }
        return ArticleText;
    }