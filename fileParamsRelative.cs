// AutoWikiBrowser module to localize and standardize file parameters in Italian Wikipedia articles
// by User:super nabla, December 2025

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
                tokens[0] = Regex.Replace(filePrefix, @"^(Image|image):", "File:", RegexOptions.IgnoreCase);
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
            
            // 4. Logica di conversione PX -> verticale (solo se c'è 'min')
            if (pxParameter != null)
            {
                if (replacedParameters.Contains("min"))
                {
                    // Se c'è 'min' (o se è stato convertito in 'min'), converte px in verticale
                    try
                    {
                        // Ottieni le dimensioni reali dell'immagine
                        var ratio = GetImageRatio(tokens[0]);
                        if (ratio > 0)
                        {
                            
                            // Calcola il valore verticale: 
                            double verticale_val = Math.Sqrt(ratio * 0.75);
                            verticale_val = Math.Round(verticale_val, 2);
                            
                            // Aggiungi il parametro verticale
                            string verticaleParam = "verticale=" + verticale_val.ToString();
                            
                            // Inserisci il parametro verticale dopo 'min' se presente
                            int minIndex = replacedParameters.IndexOf("min");
                            if (minIndex > -1)
                            {
                                replacedParameters.Insert(minIndex + 1, verticaleParam);
                            }
                            else
                            {
                                replacedParameters.Insert(0, verticaleParam);
                            }
                            
                            nSubstitutions++;
                            pxRemoved = true; // Segna che è avvenuta una rimozione di 'px'
                        }
                    }
                    catch
                    {
                        // In caso di errore, mantieni il parametro px (fallback)
                        replacedParameters.Insert(0, pxParameter);
                    }
                }
                else
                {
                    // Se NON c'è 'min', mantieni il parametro px
                    replacedParameters.Insert(0, pxParameter);
                }
            }
            
            // Se c'è 'min', rimuovi 'destra'
            if (replacedParameters.Contains("min"))
            {
                int indexDestra = replacedParameters.IndexOf("destra");
                if (indexDestra > -1)
                {
                    replacedParameters.RemoveAt(indexDestra);
                }
            }
            
            // Standardizzazione di 'verticale'
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
        
        // Salta l'articolo se non sono state apportate modifiche
        if (! pxRemoved)
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
                Summary += " " + "Il [[WP:RIS|dimensionamento assoluto]] delle immagini è deprecato (sostituito con un ridimensionamento relativo; cfr.: [[WP:Bot/Autorizzazioni]]).";
            }
        }
        return ArticleText;
    }

// Funzione per ottenere le dimensioni dell'immagine via API
private double GetImageRatio(string imageTitle)
{
    try
    {       
        // Esegui la chiamata API a MediaWiki
        string url = "https://www.mediawiki.org/w/api.php";
        string parameters = string.Format(
            "?action=query&prop=imageinfo&iiprop=size&titles={0}&format=json",
            System.Web.HttpUtility.UrlEncode(imageTitle)
        );
        
        using (System.Net.WebClient client = new System.Net.WebClient())
        {
            client.Headers.Add("User-Agent", "AutoWikiBrowser Custom Module ([[it:user:nablabot]]); contact at [[it:user talk:super nabla]]");
            string json = client.DownloadString(url + parameters);
            
            // Parsing del JSON (semplificato)
            if (json.Contains("\"width\":"))
            {
                // Estrai width
                int widthIndex = json.IndexOf("\"width\":") + 8;
                int widthEndIndex = json.IndexOf(",", widthIndex);
                string widthStr = json.Substring(widthIndex, widthEndIndex - widthIndex).Trim();
                
                // Estrai height
                int heightIndex = json.IndexOf("\"height\":", widthEndIndex) + 9;
                int heightEndIndex = json.IndexOf("}", heightIndex);
                string heightStr = json.Substring(heightIndex, heightEndIndex - heightIndex).Trim();
                
                int width, height;
                if (int.TryParse(widthStr, out width) && int.TryParse(heightStr, out height))
                {
                    return 1.0 * width / height;
                }
            }
        }
    }
    catch
    {
        // In caso di errore, restituisci -1
    }
    
    return -1;
}