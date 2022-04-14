grammar Story;

program: player code* story EOF;

player: 'PLAYER''{'attribute*'}';

attribute: IDENTIFIER ':' expression';';

expression
    : literal                           #literalExpression
    | 'PLAYER.'IDENTIFIER               #playerCallExpression
    | IDENTIFIER                        #variableCallExpression
    | '(' expression ')'                #paranthesizedExpression
    | '!' expression                    #notExpression
    | expression MULTOP expression      #multiplicativeExpression
    | expression ADDOP expression       #additiveExpression
    | expression COMPAREOP expression   #comparisonExpression
    | expression BOOLOP expression      #booleanExpression
    ;

code: variableDeclaration | assignment;

variableDeclaration: 'var 'assignment;
assignment: IDENTIFIER '=' expression ';';

story: start scene* end;

start: SCENE START'{'write (choices | goTo)'}';
end: SCENE END'{'write'}';
scene: SCENE IDENTIFIER'{'write (choices | goTo)'}';

write: 'write'':'expression';';

choices:'choices''{'choice+'}';

choice: IDENTIFIER'{'choiceText goTo'}';

choiceText: 'text'':'expression';';

goTo: 'goto'':'(IDENTIFIER | START | END)';';

literal: number | STRING | BOOL;

number: INT | INT'.'INT;

MULTOP: '*' | '/';
ADDOP: '+' | '-';
COMPAREOP: '==' | '!=' | '>' | '<' | '>=' | '<=';
BOOLOP: 'and' | 'or';

INT: [0-9]+;
STRING: ('"' ~'"'* '"') | ('\'' ~'\''* '\'');
BOOL: 'true' | 'false';

WS: [ \t\r\n]+ -> skip;
START: 'START';
END: 'END';
SCENE: 'SCENE ';
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;