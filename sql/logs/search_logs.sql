/* Your Logs API */
drop procedure if exists SearchLogs;
DELIMITER ^_^
CREATE PROCEDURE SearchLogs(
    search_term varchar(250),
    is_archived bit,
    is_deleted bit,
    is_enabled bit
)
BEGIN
    #     compute temp vars first
    #     set @search_term = coalesce(search_term, '%');

    set @search_term = IF(TRIM(COALESCE(search_term, '')) = '', '%', search_term);
    #     select @search_term;

    #     set @regex_symbols = '[[\{\}\.\s\d\w]\+?\*?]+';
    #     set @regex_symbols = '(\\[sdw]|[.])[+*?]*(\{\d*,?\d*\})?';
    set @regex_symbols = '^(\\[sdw]|[.])[+*?]*$';
    #     set @search_term = 'Hello there! \\s \\d \\w \\.';
    -- https://regex101.com/r/YYkqhs/1
    set @swapped = REGEXP_REPLACE(concat(@search_term, ''), @regex_symbols, '');
    #     set @symbols_found = regexp_substr(search_term, @regex_symbols);
    set @regex_symbol_count = abs(length(@search_term) - length(@swapped));
    set @is_regex_search = @regex_symbol_count > 0;
    -- debug
    #     select -- @symbols_found,
    #            @regex_symbol_count,
    #            @search_term,
    #            @swapped,
    #            @is_regex_search;

    # Aggregate anything you wish to search with wildcards (%).
    set @all_text = concat(
            @search_term
        );

    # wildcards can come from user inputs or COALESCES
    set @wildcard_count = abs(LENGTH(@all_text) - LENGTH(REPLACE(@all_text, '%', '')));

    # Disabwildcard_countle full text search if search term is empty
    set @use_full_text_search = IF(TRIM(COALESCE(@search_term, '')) = '', 0, 1);
    # Toggle exact match search
    -- if full-text search is on, we don't want to match exact.
    set @match_exact =
            case
                when
                    (@wildcard_count = 0 AND @use_full_text_search = 0 and @is_regex_search = 0) then 1
                else
                    @wildcard_count = 0
                end;
    #debug

    #     set @debug_mode = 1;
#     case when @debug_mode = 1  then (
#     select @search_term
#          , @all_text
#          , @is_regex_search
#          , @regex_symbol_count
#          , @swapped
#          , @wildcard_count
#          , @match_exact
#     ) else 0 end;

    SELECT logrow.exception_message
         , logrow.exception_text
         , logrow.application_name
         , logrow.created_by
         , logrow.modified_by
         , logrow.is_archived
         , logrow.is_enabled
         , logrow.is_deleted
         #          , MATCH(logrow.created_by, logrow.modified_by, logrow.exception_text)
         # #                  AGAINST(@search_term IN NATURAL LANGUAGE MODE) AS score
         #                 AGAINST(@search_term IN BOOLEAN MODE) AS score
         , @match_exact
         , @use_full_text_search
         , @is_regex_search
         , @wildcard_count
         , @search_term
    FROM logs logrow
    where 1 = 1
        # text filters
        AND (
                          @match_exact = 1 AND (
                              logrow.created_by = @search_term
                          OR logrow.modified_by = @search_term
                          OR logrow.exception_text = @search_term
                      )
                  XOR
                          @match_exact = 0 AND (
                                      logrow.created_by like @search_term
                                  OR logrow.modified_by like @search_term
                                  OR logrow.exception_text like @search_term
                              )
              )
       #         XOR @is_regex_search = 1 AND (
       #                     logrow.created_by regexp @search_term
       #                 OR logrow.modified_by regexp @search_term
       #                 OR logrow.exception_text regexp @search_term
       #             )
       #        # flags
       OR (
                      is_archived is not null and logrow.is_archived = is_archived
                  xor
                      is_deleted is not null and
                      logrow.is_deleted = is_deleted
                  xor
                      is_enabled is not null and
                      logrow.is_enabled = is_enabled
              )
        xor (@use_full_text_search = 1 and MATCH(logrow.created_by, logrow.modified_by, logrow.exception_text)
                                                 AGAINST(@search_term IN NATURAL LANGUAGE MODE))

    #     order by score desc
    ;
END ^_^

DELIMITER ;

-- full-text search
call SearchLogs('Nick P 2', null, null, null);
-- exact search
call SearchLogs('Nick Preston', null, null, null);
-- fuzzy + flags
call SearchLogs('Alan Preston', null, false, 0);
-- todo: figure out a way to pad short words with %.

-- get everything when null
call SearchLogs(null, null, null, null);
-- get everything when empty
call SearchLogs('', null, null, null);
-- get everything via wildcard
call SearchLogs('%ick%', null, null, null);

/* 
-- TEST TEXT VARIANTS
   */

/*
    -- regecall SearchLogs('%ick%', null, null, null);
x spaces
    call SearchLogs('Braden Preston\s', null, null, null);
    -- regex digits
    call SearchLogs('\\d', null, null, null);
    -- Allowing for regex searches
    call SearchLogs('\w*l.*', null, null, null);
    -- nothing is right, no rows.
    call SearchLogs(null, 0, 0, 0);
    -- wrong name, right flag.
    call SearchLogs('Nick', 1, null, null);
   
    -- should get Braden
    call SearchLogs('Braden Preston', null, null, null);
    -- Wildcard search for Alan
    call SearchLogs('%NeW%', null, null, null);
    -- Wildcard search for Braden
    call SearchLogs('BrAd%', null, null, null);
    */
/* 
-- Test our flag filters
-- archived only
call SearchLogs(null,  1, 0, 0);
-- deleted only
call SearchLogs(null,  0, 1, 0);
-- enabled only
call SearchLogs(null, 0, 0, 1);
-- it will never be all 3
call SearchLogs(null, 1, 1, 1);
call SearchLogs(null, null, null, null);
   */

# 
/*
select *
             from logs
      where exception_text
                REGEXP 'Arg.*'
          and issue_url REGEXP 'jira2'
      ;
 */
